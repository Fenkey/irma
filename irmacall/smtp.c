#include <sys/types.h>
#include <sys/stat.h>
#include <fcntl.h>
#include <unistd.h>
#include <assert.h>
#include <string.h>
#include "wrapper.h"
#include "param.h"
#include "service.h"
#include "smtp.h"

#define ATTACHMENT_MAX 2621440 // 2.5M
#define SI ((smtp_inner_t*)s->priv)

typedef struct {
	CURL *curl;
	buf_t *server;
	buf_t *user;
	buf_t *password;
	buf_t *h_from;
	buf_t *h_to;
	buf_t *h_subject;
	buf_t *buf;
	buf_t *b64;
	paramparser_t *parser;
	paramlist_t *plist;
} smtp_inner_t;

static smtp_inner_t* new_smtp_inner(buf_pool_t *pool, const char *server, const char *user, const char *password)
{
	smtp_inner_t *si = xcalloc(1, sizeof(*si));
	si->curl = curl_easy_init();
	if (!si->curl) {
		free(si);
		return NULL;
	}
	si->server = pool->lend(pool, 0, 0);
	buf_printf(si->server, server);
	si->user = pool->lend(pool, 0, 0);
	buf_printf(si->user, user);
	si->password = pool->lend(pool, 0, 0);
	buf_printf(si->password, password);
	si->h_from = pool->lend(pool, 0, 0);
	si->h_to = pool->lend(pool, 0, 0);
	si->h_subject = pool->lend(pool, 0, 0);
	si->buf = pool->lend(pool, 0, 0);
	si->b64 = pool->lend(pool, 0, 0);
	si->parser = paramparser_new(pool);
	si->plist = si->parser->paramlist_new(si->parser);
	return si;
}

static void free_smtp_inner(smtp_inner_t *si)
{
	if (si->curl)
		curl_easy_cleanup(si->curl);
	if (si->parser) {
		if (si->plist)
			si->parser->paramlist_free(si->plist);
		paramparser_free(si->parser, 0);
	}
	if (si->server)
		buf_return(si->server);
	if (si->user)
		buf_return(si->user);
	if (si->password)
		buf_return(si->password);
	if (si->h_from)
		buf_return(si->h_from);
	if (si->h_to)
		buf_return(si->h_to);
	if (si->h_subject)
		buf_return(si->h_subject);
	if (si->buf)
		buf_return(si->buf);
	if (si->b64)
		buf_return(si->b64);
	free(si);
}

static void parse_cb(param_t *p, buf_t *buf)
{
	buf_trim(p->key);
	if (p->key->offset <= 0)
		buf_reset(p->value);
	else if (p->key->data[0] != '<' && p->key->data[p->key->offset-1] != '>')
		buf_printf(p->value, "<%s>", p->key->data);
	else
		buf_copy(p->key, p->value);
}

static void print(param_t *p, buf_t *buf)
{
	if (buf->offset > 0)
		buf_printf_ext(buf, ",%s", p->value->data);
	else
		buf_printf_ext(buf, "To: %s", p->value->data);
}

static struct curl_slist* headers_fill(const char *from, const char *to, const char *subject)
{
	struct curl_slist *headers = NULL;
	if (to)
		headers = curl_slist_append(headers, to);
	headers = curl_slist_append(headers, from);
	headers = curl_slist_append(headers, subject);
	return headers;
}

static struct curl_slist* recipients_fill(paramlist_t *plist)
{
	int i = 0;
	struct curl_slist *recipients = NULL;
	for (; i < plist->count(plist); i++) {
		param_t *p = plist->get(plist, i);
		if (p->value->offset > 0)
			recipients = curl_slist_append(recipients, p->value->data);
	}
	return recipients;
}

static char* b64enc(unsigned char *s, int len, char *c)
{
	static const char t64[] = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
	int n = len / 3, r = len % 3, i;
	for (i = 0; i < n; i++) {
		*c++ = t64[(*s>>2)&0x3f];
		*c++ = t64[((*s<<4)&0x30)|((*(s+1)>>4)&0x0f)];
		*c++ = t64[((*(s+1)<<2)&0x3c)|((*(s+2)>>6)&0x03)];
		*c++ = t64[*(s+2)&0x3f];
		s += 3;
	}
	if (r == 2) {
		*c++ = t64[(*s>>2)&0x3f];
		*c++ = t64[((*s<<4)&0x30)|((*(s+1)>>4)&0x0f)];
		*c++ = t64[(*(s+1)<<2)&0x3c];
		*c++ = '=';
	} else if (r == 1) {
		*c++ = t64[(*s>>2)&0x3f];
		*c++ = t64[(*s<<4)&0x30];
		*c++ = '=';
		*c++ = '=';
	}
	//*c++ = '\0';
	return c;
}

static buf_t* from_localfile(const char *file, buf_t *buf, const char **fname, char *error)
{
	int fd = open(file, O_RDONLY);
	if (fd < 0) {
		sprintf(error, "fail to read local attachment: %s", file);
		return NULL;
	}

	struct stat st;
	if (fstat(fd, &st) < 0 || st.st_size <= 0 || st.st_size > ATTACHMENT_MAX) {
		close(fd);
		sprintf(error, "attachment size invalid: %s", file);
		return NULL;
	}

	int n;
	char *p = buf_data(buf, st.st_size);
	buf_reset(buf);
	while ((n = read(fd, p, buf->size - buf->offset)) > 0) {
		p += n;
		buf->offset += n;
	}
	close(fd);

	p = strrchr(file, '/');
	*fname = p ? p + 1 : file;

	return buf;
}

static buf_t* from_postfile(int index, const char **fname, char *error)
{
	worker_t *w = CURRENT;

	int file_count = 0;
	if (w->request_post_parse(w, &file_count) <= 0 || file_count <= index) {
		sprintf(error, "post attachment not found: [%d]", index);
		return NULL;
	}

	buf_t *buf;
	if (!w->request_file_post_param_by_index_b(w, index, &buf, fname, NULL) || !buf) {
		sprintf(error, "fail to read post attachment: [%d]", index);
		return NULL;
	}

	if (buf->offset > ATTACHMENT_MAX) {
		sprintf(error, "attachment size invalid: [%d]", index);
		return NULL;
	}

	return buf;
}

static int content_fill(smtp_t *s, curl_mime *mime)
{
	curl_mimepart *part = NULL;
	if (s->content->offset > 0) {
		part = curl_mime_addpart(mime);
		curl_mime_data(part, s->content->data, CURL_ZERO_TERMINATED);
		curl_mime_type(part, "text/html");
	}

	buf_t *buf;
	const char *fname;
	int i = 0, ret = -1;
	struct curl_slist *slist = NULL;

	for (; i < sizeof(s->attachment)/sizeof(*s->attachment); i++) {
		if (s->attachment[i]->offset <= 0)
			continue;

		if (s->attachment[i]->offset == 1 && s->attachment[i]->data[0] == '-')
			buf = from_postfile(i, &fname, s->error);
		else
			buf = from_localfile(s->attachment[i]->data, SI->buf, &fname, s->error);

		if (!buf)
			goto __exit;

		part = curl_mime_addpart(mime);
		slist = curl_slist_append(NULL, "Content-Transfer-Encoding: Base64");
		curl_mime_headers(part, slist, 1);
		curl_mime_name(part, fname);

		/*
		 * FIX: Don't append '\0' to the tail of b64 because it will make decode failed in libcurl
		 * (refer to read_encoded_part_content() of lib/mime.c)
		 * int b64_size = (((buf->offset+2)/3)<<2)+1;
		 */
		int b64_size = (((buf->offset+2)/3)<<2);
		buf_data(SI->b64, b64_size);
		b64enc((unsigned char*)buf->data, buf->offset, SI->b64->data);
		curl_mime_data(part, SI->b64->data, b64_size);
	}
	ret = 0;

__exit:
	buf_force_reset(SI->buf);
	buf_force_reset(SI->b64);
	return ret;
}

static int smtp_mail(smtp_t *s, int hideto, int verbose)
{
	assert(s && s->priv && SI->curl);

	int ret = -1;
	*s->error = '\0';
	if (s->to->offset <= 0 || s->subject->offset <= 0) {
		strcpy(s->error, "Invalid 'to' or 'subject'");
		return ret;
	}

	if (SI->parser->parse(SI->plist, s->to->data, s->to->offset, ',', '\0', &parse_cb) <= 0) {
		strcpy(s->error, "Parse 'to' data wrong");
		return ret;
	}
	buf_printf(SI->h_from, "From: %s", SI->user->data);
	buf_printf(SI->h_subject, "Subject: %s\r\n", s->subject->data);
	if (!hideto)
		SI->plist->print(SI->plist, &print, SI->h_to);

	/*
	 * curl_easy_reset():
	 * Re-initializes all options previously set on a specified CURL handle to the default values.
	 * This puts back the handle to the same state as it was in when it was just created with curl_easy_init.
	 * It does not change the following information kept in the handle: live connections, the Session ID cache,
	 * the DNS cache, the cookies and shares.
	 */
	curl_easy_reset(SI->curl);
	/*
	 * This is the URL for your mailserver. Note the use of smtps:// rather
	 * than smtp:// to request a SSL based connection. Such as:
	 * "smtp://smtp.exmail.qq.com:25"
	 * "smtps://smtp.exmail.qq.com:465"
	 */
	curl_easy_setopt(SI->curl, CURLOPT_URL, SI->server->data);
	curl_easy_setopt(SI->curl, CURLOPT_USERNAME, SI->user->data);
	curl_easy_setopt(SI->curl, CURLOPT_PASSWORD, SI->password->data);
	curl_easy_setopt(SI->curl, CURLOPT_SSL_VERIFYPEER, 0L);
	curl_easy_setopt(SI->curl, CURLOPT_SSL_VERIFYHOST, 0L);
	/*
	 * Note that this option isn't strictly required, omitting it will result
	 * in libcurl sending the MAIL FROM command with empty sender data. All
	 * autoresponses should have an empty reverse-path, and should be directed
	 * to the address in the reverse-path which triggered them. Otherwise,
	 * they could cause an endless loop. See RFC 5321 Section 4.5.5 for more
	 * details.
	 */
	curl_easy_setopt(SI->curl, CURLOPT_MAIL_FROM, SI->user->data);

	struct curl_slist *recipients = recipients_fill(SI->plist);
	curl_easy_setopt(SI->curl, CURLOPT_MAIL_RCPT, recipients);

	struct curl_slist *headers = headers_fill(SI->h_from->data, hideto ? NULL : SI->h_to->data, SI->h_subject->data);
	curl_easy_setopt(SI->curl, CURLOPT_HTTPHEADER, headers);

	curl_mime *mime = curl_mime_init(SI->curl);
	if (content_fill(s, mime) < 0)
		goto __exit;
	curl_easy_setopt(SI->curl, CURLOPT_MIMEPOST, mime);

	if (g_shareobj)
		curl_easy_setopt(SI->curl, CURLOPT_SHARE, (CURLSH*)g_shareobj);
	curl_easy_setopt(SI->curl, CURLOPT_DNS_CACHE_TIMEOUT, DNS_CACHE_TIMEOUT);

	if (verbose)
		curl_easy_setopt(SI->curl, CURLOPT_VERBOSE, 1L);

	CURLcode res = curl_easy_perform(SI->curl);
	if (res != CURLE_OK)
		strcpy(s->error, curl_easy_strerror(res));
	else
		ret = 0;

__exit:
	curl_slist_free_all(headers);
	curl_slist_free_all(recipients);
	curl_mime_free(mime);
	return ret;
}

static void smtp_clean(smtp_t *s)
{
	buf_reset(s->to);
	buf_reset(s->subject);
	buf_reset(s->content);
	int i = 0;
	for (; i < sizeof(s->attachment)/sizeof(*s->attachment); i++)
		buf_reset(s->attachment[i]);
	*s->error = '\0';
	buf_reset(SI->h_from);
	buf_reset(SI->h_to);
	buf_reset(SI->h_subject);
	buf_reset(SI->buf);
	buf_reset(SI->b64);
	SI->plist->reset(SI->plist);
}

smtp_t* smtp_new(buf_pool_t *pool, const char *server, const char *user, const char *password)
{
	assert(pool && server);
	if (!user || !password)
		return NULL;
	smtp_inner_t *si = new_smtp_inner(pool, server, user, password);
	if (!si)
		return NULL;
	smtp_t *s = xcalloc(1, sizeof(*s));
	s->priv = si;
	s->to = pool->lend(pool, 0, 0);
	s->subject = pool->lend(pool, 0, 0);
	s->content = pool->lend(pool, 0, 0);
	int i = 0;
	for (; i < sizeof(s->attachment)/sizeof(*s->attachment); i++)
		s->attachment[i] = pool->lend(pool, 0, 0);
	s->mail = &smtp_mail;
	s->clean = &smtp_clean;
	return s;
}

void smtp_free(void *val)
{
	assert(val);
	smtp_t *s = (smtp_t*)val;
	if (s->priv)
		free_smtp_inner(SI);
	if (s->to)
		buf_return(s->to);
	if (s->subject)
		buf_return(s->subject);
	if (s->content)
		buf_return(s->content);
	int i = 0;
	for (; i < sizeof(s->attachment)/sizeof(*s->attachment); i++)
		buf_return(s->attachment[i]);
	free(s);
}
