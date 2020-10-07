#include <unistd.h>
#include <string.h>
#include <assert.h>
#include <errno.h>
#include <sys/select.h>
#include <sys/time.h>
#include "wrapper.h"
#include "misc.h"
#include "fetcher.h"

#define CONNECT_TIMEOUT_DEFAULT 3
#define READ_TIMEOUT_DEFAULT 16
#define FI ((fetcher_inner_t*)f->priv)

typedef struct {
	CURL *curl;
	CURLM *curlm;
	struct curl_slist *req_headers;
	curl_mime *req_formmime;
	buf_t *req_body;
	buf_t *req_cookies;
	buf_t *buf;
	int proxy;
} fetcher_inner_t;

static void fetcher_reset(fetcher_t *f)
{
	/*
	 * curl_easy_reset():
	 * Re-initializes all options previously set on a specified CURL handle to the default values. This
	 * puts back the handle to the same state as it was in when it was just created with curl_easy_init.
	 * It does not change the following information kept in the handle: live connections, the Session ID
	 * cache, the DNS cache, the cookies and shares.
	 */
	curl_easy_reset(FI->curl);
	if (FI->curlm) {
		curl_multi_cleanup(FI->curlm);
		FI->curlm = NULL;
	}
	buf_reset(f->req_url);
	buf_reset(f->res_headers);
	buf_reset(f->res_cookies);
	buf_reset(f->res_body);
	buf_reset(FI->buf);
	*f->error = '\0';
	f->time_used = 0.0;
}

static size_t read_body(char *p, size_t size, size_t n, void *private)
{
	buf_t *buf = (buf_t*)private;
	memcpy(p, buf->data, buf->offset);
	return buf->offset;
}

static size_t save_body(void *p, size_t size, size_t n, void *private)
{
	buf_t *buf = (buf_t*)private;
	size *= n;
	if (buf)
		buf_append(buf, p, size);
	return size;
}

static size_t save_headers(void *p, size_t size, size_t n, void *private)
{
	buf_t *buf = (buf_t*)private;
	size *= n;
	if (buf) {
		buf_append(buf, "\r\n", 2);
		buf_append(buf, p, size);
	}
	return size;
}

static void get_response_info(fetcher_t *f)
{
	if (curl_easy_getinfo(FI->curl, CURLINFO_RESPONSE_CODE, &f->res_code) != CURLE_OK) {
		f->res_code = -1;
		strcpy(f->error, "Get response code wrong");
	} else if (curl_easy_getinfo(FI->curl, CURLINFO_TOTAL_TIME, &f->time_used) != CURLE_OK) {
		f->res_code = -1;
		strcpy(f->error, "Get used time wrong");
	}
	if (f->res_code == 200) {
		*f->error = '\0';
		struct curl_slist *cookie = NULL;
		if (f->res_cookies && curl_easy_getinfo(FI->curl, CURLINFO_COOKIELIST, &cookie) == CURLE_OK && cookie) {
			/*
			 * Netscape format(Refer to http://www.ietf.org/rfc/rfc2109.txt):
			 * <httponly><domain>\t<tailmatch>\t<path>\t<secure>\t<expires>\t<key>\t<value>
			 * NOTE that no '\t' between <httponly> and <domain>.
			 */
			struct curl_slist *c = cookie;
			for (; c; c = c->next)
				buf_printf_ext(f->res_cookies, "%s`", c->data);
			f->res_cookies->data[f->res_cookies->offset - 1] = '\0';
			f->res_cookies->offset -= 1;
			curl_slist_free_all(cookie);
			/* Clear cookies saved in curl handle. */
			curl_easy_setopt(FI->curl, CURLOPT_COOKIE, "ALL");
		}
	}
	curl_easy_getinfo(FI->curl, CURLINFO_CONTENT_TYPE, &f->res_content_type);
}

static CURLcode multi_select(fetcher_t *f, int timeout)
{
	int running = 0;
	struct timeval t;

	while (curl_multi_perform(FI->curlm, &running) == CURLM_CALL_MULTI_PERFORM)
		;
	while (running) {
		long wait;
		curl_multi_timeout(FI->curlm, &wait);

		int maxfd;
		fd_set fdread, fdwrite, fdexcep;
		FD_ZERO(&fdread);
		FD_ZERO(&fdwrite);
		FD_ZERO(&fdexcep);
		if (curl_multi_fdset(FI->curlm, &fdread, &fdwrite, &fdexcep, &maxfd) != CURLM_OK) {
			strcpy(f->error, "Init failed");
			return -1;
		}

		/*
		 * Note: if libcurl returns a -1 timeout here, it just means that libcurl currently has no stored timeout value.
		 * You must not wait too long (more than a few seconds perhaps) before you call curl_multi_perform() again.
		 * Refer to https://curl.haxx.se/libcurl/c/curl_multi_timeout.html
		 */
		if (maxfd < 0) {
			if (wait > 0L) {
				t.tv_sec = wait / 1000;
				t.tv_usec = (wait % 1000) * 1000;
			} else {
				t.tv_sec = 0;
				t.tv_usec = 100 * 1000;
			}
			select(0, NULL, NULL, NULL, &t);
		} else {
__again:
			t.tv_sec = timeout;
			t.tv_usec = 0;
			int ret = select(maxfd + 1, &fdread, &fdwrite, &fdexcep, &t);
			if (ret < 0) {
				if (errno == EINTR)
					goto __again;
				strcpy(f->error, "Execute failed");
				return -1;
			} else if (ret == 0) {
				strcpy(f->error, "Time out");
				return -1;
			}
		}

		while (curl_multi_perform(FI->curlm, &running) == CURLM_CALL_MULTI_PERFORM)
			;
	}
	return CURLE_OK;
}

static CURLcode multi_perform(fetcher_t *f, int timeout)
{
	if (!(FI->curlm = curl_multi_init())) {
		strcpy(f->error, "Init failed");
		return -1;
	}
	curl_multi_add_handle(FI->curlm, FI->curl);
	return multi_select(f, timeout);
}

static long run(fetcher_t *f, const char *url, int urllen, method_t method, int timeout)
{
	int ret = 0;

	buf_append(f->req_url, url, urllen);
	f->req_method = method;

	if (timeout <= 0)
		timeout = READ_TIMEOUT_DEFAULT;

	switch (f->req_method) {
	case GET:
		curl_easy_setopt(FI->curl, CURLOPT_HTTPGET, 1L);
		break;

	case POST: {
		int expect = 1;

		/*
		 * We adopt the MIME APIs but not HTTPOST that refer to:
		 * https://curl.haxx.se/libcurl/c/CURLOPT_HTTPPOST.html
		 * ".. This option is deprecated! Do not use it. Use CURLOPT_MIMEPOST instead after having prepared mime data."
		 * Others reference:
		 * https://curl.haxx.se/libcurl/c/CURLOPT_MIMEPOST.html
		 * https://curl.haxx.se/libcurl/c/smtp-mime.html
		 */

		if (FI->req_formmime)
			curl_easy_setopt(FI->curl, CURLOPT_MIMEPOST, FI->req_formmime);
		else {
			curl_easy_setopt(FI->curl, CURLOPT_POST, 1L);
			if (FI->req_body->offset > 0) {
				curl_easy_setopt(FI->curl, CURLOPT_POSTFIELDS, (void*)FI->req_body->data);
				curl_easy_setopt(FI->curl, CURLOPT_POSTFIELDSIZE, FI->req_body->offset);
			} else
				expect = 0;
		}

		if (expect) {
			/*
			 * Before posting a body with size beyonds 1024 bytes, libcurl will send a "Expect:100-continue"
			 * header request to the server for querying whether server will accept it, and client post the
			 * body while it receivs the response of "100-continue". We can ignore and disable it:
			 */
			f->append_header(f, "Expect:");
		}
		break;
	}

	case PUT:
		curl_easy_setopt(FI->curl, CURLOPT_PUT, 1L);
		if (FI->req_body->offset > 0) {
			curl_easy_setopt(FI->curl, CURLOPT_READDATA, (void*)FI->req_body);
			curl_easy_setopt(FI->curl, CURLOPT_INFILESIZE_LARGE, (curl_off_t)FI->req_body->offset);
			curl_easy_setopt(FI->curl, CURLOPT_READFUNCTION, &read_body);
			f->append_header(f, "Expect:");
		}
		break;

	case HEAD:
		curl_easy_setopt(FI->curl, CURLOPT_NOBODY, 1L);
		break;

	case DELETE:
		curl_easy_setopt(FI->curl, CURLOPT_CUSTOMREQUEST, "DELETE");
		break;

	default:
		strcpy(f->error, "Invalid method");
		ret = -1;
		goto __out;
	}

	/*
	 * Maybe it isn't a good idea to set CURLOPT_FORBID_REUSE. When we reuse an idle connection, perform()
	 * will return CURLE_OPERATION_TIMEDOUT (28) and cause the remote client to close and change the state
	 * to FIN_WAIT1/2. In addition, we don't know when is the best time to fresh a connection by setting
	 * CURLOPT_FRESH_CONNECT. Any suggestions ?
	 */
	curl_easy_setopt(FI->curl, CURLOPT_FORBID_REUSE, 1L);
	curl_easy_setopt(FI->curl, CURLOPT_URL, f->req_url->data);
	//curl_easy_setopt(FI->curl, CURLOPT_CONNECTTIMEOUT, CONNECT_TIMEOUT_DEFAULT);
	//curl_easy_setopt(FI->curl, CURLOPT_TIMEOUT, timeout);
	curl_easy_setopt(FI->curl, CURLOPT_SSL_VERIFYPEER, 0L);
	curl_easy_setopt(FI->curl, CURLOPT_SSL_VERIFYHOST, 0L);
	curl_easy_setopt(FI->curl, CURLOPT_ERRORBUFFER, f->error);
	curl_easy_setopt(FI->curl, CURLOPT_WRITEDATA, f->res_body);
	curl_easy_setopt(FI->curl, CURLOPT_WRITEFUNCTION, &save_body);
	curl_easy_setopt(FI->curl, CURLOPT_FOLLOWLOCATION, 1L);
	/*
	 * Avoid causing any potential clash risk on signals between libcurl and mono.
	 * "In unix-like systems, this might cause signals to be used unless CURLOPT_NOSIGNAL is set."
	 * refer to https://curl.haxx.se/libcurl/c/CURLOPT_TIMEOUT.html
	 */
	curl_easy_setopt(FI->curl, CURLOPT_NOSIGNAL, 1L);
	/* This option was called CURLOPT_ACCEPT_ENCODING after 7.21.6 */
	curl_easy_setopt(FI->curl, CURLOPT_ACCEPT_ENCODING, "gzip,deflate");
	curl_easy_setopt(FI->curl, CURLOPT_TRANSFER_ENCODING, FI->proxy ? 0L : 1L);
	curl_easy_setopt(FI->curl, CURLOPT_HTTP_CONTENT_DECODING, FI->proxy ? 0L : 1L);
	/*
	 * Don't invoke the code as below ! Refer to https://curl.haxx.se/libcurl/c/CURLOPT_COOKIEFILE.html
	 *
	 * "It also enables the cookie engine, making libcurl parse and send cookies on subsequent requests with this handle.
	 * Given an empty or non-existing file or by passing the empty string ("") to this option, you can enable the cookie
	 * engine without reading any initial cookies. "
	 * "If you use the Set-Cookie format and don't specify a domain then the cookie is sent for any domain (even after
	 * redirects are followed) and cannot be modified by a server-set cookie. If a server sets a cookie of the same name
	 * then both will be sent on a future transfer to that server, likely not what you intended."
	 *
	 * curl_easy_reset can't clean up the cached cookies unless rebuild the handle via curl_easy_cleanup & curl_easy_init.
	 */
	//curl_easy_setopt(FI->curl, CURLOPT_COOKIEFILE, "");
	if (FI->req_headers)
		curl_easy_setopt(FI->curl, CURLOPT_HTTPHEADER, FI->req_headers);
	if (FI->req_cookies->offset > 0)
		curl_easy_setopt(FI->curl, CURLOPT_COOKIE, FI->req_cookies->data);
	if (f->res_headers) {
		curl_easy_setopt(FI->curl, CURLOPT_WRITEHEADER, f->res_headers);
		curl_easy_setopt(FI->curl, CURLOPT_HEADERFUNCTION, &save_headers);
	}
	if (g_shareobj)
		curl_easy_setopt(FI->curl, CURLOPT_SHARE, (CURLSH*)g_shareobj);
	curl_easy_setopt(FI->curl, CURLOPT_DNS_CACHE_TIMEOUT, DNS_CACHE_TIMEOUT);

	CURLcode code = multi_perform(f, timeout);
	if (code == CURLE_OK)
		get_response_info(f);
	else {
		f->res_code = (int)code;
		if (!*f->error)
			strcpy(f->error, curl_easy_strerror(code));
	}
	ret = f->res_code;
	
__out:
	f->clear_headers(f);
	f->clear_formpost(f);
	buf_force_reset(FI->req_body);
	buf_reset(FI->req_cookies);
	FI->proxy = 0;
	return ret;
}

/* Header format: "key:value" */
static int fetcher_append_header(fetcher_t *f, const char *header)
{
	if (header) {
		FI->req_headers = curl_slist_append(FI->req_headers, header);
		if (start_with(header, "X-IRMAKIT-PROXY:", 0))
			FI->proxy = 1;
	}
	return 0;
}

static int fetcher_clear_headers(fetcher_t *f)
{
	FI->proxy = 0;
	if (FI->req_headers) {
		curl_slist_free_all(FI->req_headers);
		FI->req_headers = NULL;
	}
	return 0;
}

static int fetcher_append_formpost_kv(fetcher_t *f, const char *key, const char *value)
{
	if (!f || !key || !value)
		return -1;
	if (!FI->req_formmime)
		FI->req_formmime = curl_mime_init(FI->curl);
	curl_mimepart *part = curl_mime_addpart(FI->req_formmime);

	buf_printf(FI->buf, "Content-Disposition: form-data; name=\"%s\"", key);
	struct curl_slist *s = curl_slist_append(NULL, FI->buf->data);
	curl_mime_headers(part, s, 1);
	curl_mime_data(part, value, CURL_ZERO_TERMINATED);
	return 0;
}

static int fetcher_append_formpost_file(fetcher_t *f, const char *name, const char *file, const char *content_type)
{
	if (!f || !file)
		return -1;
	if (access(file, F_OK) < 0) {
		strcpy(f->error, "Open file failed");
		return -1;
	}
	if (!name) {
		name = strrchr(file, '/');
		name = name ? name + 1 : file;
	}
	if (!FI->req_formmime)
		FI->req_formmime = curl_mime_init(FI->curl);
	curl_mimepart *part = curl_mime_addpart(FI->req_formmime);
	curl_mime_filedata(part, file);
	curl_mime_name(part, name);
	curl_mime_type(part, content_type ? content_type : "application/octet-stream");
	return 0;
}

static int fetcher_append_formpost_filebuf(fetcher_t *f, const char *name, const char *file, const char *body, long len, const char *content_type)
{
	if (!f || !file || !body || len <= 0)
		return -1;

	if (!name) {
		name = strrchr(file, '/');
		name = name ? name + 1 : file;
	}

	buf_reset(FI->req_body);
	buf_append(FI->req_body, body, len);

	if (!FI->req_formmime)
		FI->req_formmime = curl_mime_init(FI->curl);
	curl_mimepart *part = curl_mime_addpart(FI->req_formmime);
	curl_mime_data(part, FI->req_body->data, FI->req_body->offset);
	/*
	 * We set Content-Type neither use:
	 * curl_mime_type(part, content_type ? content_type : "application/octet-stream");
	 * nor use:
	 * s = curl_slist_append(s, FI->buf->data);
	 * curl_mime_headers(part, s, 2);
	 * Because it isn't expected for irma that the 'Content-Disposition' is behind 'Content-Type'.
	 */
	buf_printf(FI->buf, \
		"Content-Disposition: form-data; name=\"%s\"; filename=\"%s\"\r\n"
		"Content-Type: %s", name, file, content_type ? content_type : "application/octet-stream");
	struct curl_slist *s = curl_slist_append(NULL, FI->buf->data);
	curl_mime_headers(part, s, 1);
	return 0;
}

static int fetcher_clear_formpost(fetcher_t *f)
{
	if (FI->req_formmime) {
		curl_mime_free(FI->req_formmime);
		FI->req_formmime = NULL;
	}
	return 0;
}

static long fetcher_get(fetcher_t *f, const char *url, int timeout)
{
	if (!url)
		return -1;
	int urllen = strlen(url);
	if (urllen <= 0)
		return -1;
	fetcher_reset(f);
	return run(f, url, urllen, GET, timeout);
}

static long fetcher_post(fetcher_t *f, const char *url, const char *body, int len, int timeout)
{
	/*
	 * Make sure that the params in body have been encoded and been combined with '&'
	 * and set the header: "Content-Type:application/x-www-form-urlencoded".
	 */
	if (!url)
		return -1;
	int urllen = strlen(url);
	if (urllen <= 0)
		return -1;
	fetcher_reset(f);
	/*
	 * According to the protocol, non-empty body is required in a posting request. But it
	 * isn't always true in reality. Anyway :-(
	 */
	if (body && len > 0)
		buf_append(FI->req_body, body, len);
	return run(f, url, urllen, POST, timeout);
}

static long fetcher_postform(fetcher_t *f, const char *url, int timeout)
{
	/*
	 * It's quite different between POSTFORM and POST. For example:
	 * curl http://localhost:8080 -F"file=@1.log" -F"author=Fenkey" -F"year=2020"
	 * Then you will get something like:
	 *
	 * POST / HTTP/1.1
	 * Host: localhost:8080
	 * ...
	 * Content-Length: 468
	 * Content-Type: multipart/form-data; boundary=------------------------7e915ea1946b3d3a
	 *
	 * --------------------------7e915ea1946b3d3a
	 * Content-Disposition: form-data; name="file"; filename="1.log"
	 * Content-Type: text/plain
	 *
	 * abcdefg
	 * --------------------------7e915ea1946b3d3a
	 * Content-Disposition: form-data; name="author"
	 * Content-Type: application/octet-stream
	 *
	 * Fenkey
	 * --------------------------7e915ea1946b3d3a
	 * Content-Disposition: form-data; name="year"
	 * Content-Type: application/octet-stream
	 *
	 * 2020
	 * --------------------------7e915ea1946b3d3a--
	 */
	if (!url || !FI->req_formmime)
		return -1;
	int urllen = strlen(url);
	if (urllen <= 0)
		return -1;
	fetcher_reset(f);
	return run(f, url, urllen, POST, timeout);
}

static long fetcher_put(fetcher_t *f, const char *url, const char *body, int len, int timeout)
{
	if (!url)
		return -1;
	int urllen = strlen(url);
	if (urllen <= 0)
		return -1;
	fetcher_reset(f);
	if (body && len > 0)
		buf_append(FI->req_body, body, len);
	return run(f, url, urllen, PUT, timeout);
}

static long fetcher_delete(fetcher_t *f, const char *url, int timeout)
{
	if (!url)
		return -1;
	int urllen = strlen(url);
	if (urllen <= 0)
		return -1;
	fetcher_reset(f);
	return run(f, url, urllen, DELETE, timeout);
}

static fetcher_inner_t* new_fetcher_inner(buf_pool_t *pool)
{
	fetcher_inner_t *fi = xcalloc(1, sizeof(*fi));
	fi->curl = curl_easy_init();
	if (!fi->curl) {
		free(fi);
		return NULL;
	}
	fi->req_body = pool->lend(pool, 0, 0);
	fi->req_cookies = pool->lend(pool, 0, 0);
	fi->buf = pool->lend(pool, 0, 0);
	return fi;
}

static void free_fetcher_inner(fetcher_inner_t *fi)
{
	if (fi->curl)
		curl_easy_cleanup(fi->curl);
	if (fi->curlm)
		curl_multi_cleanup(fi->curlm);
	if (fi->req_headers)
		curl_slist_free_all(fi->req_headers);
	if (fi->req_formmime)
		curl_mime_free(fi->req_formmime);
	if (fi->req_body)
		buf_return(fi->req_body);
	if (fi->req_cookies)
		buf_return(fi->req_cookies);
	if (fi->buf)
		buf_return(fi->buf);
	free(fi);
}

fetcher_t* fetcher_new(buf_pool_t *pool)
{
	fetcher_inner_t *fi = new_fetcher_inner(pool);
	if (!fi)
		return NULL;
	fetcher_t *f = xcalloc(1, sizeof(*f));
	f->priv = fi;
	f->req_method = GET;
	f->req_url = pool->lend(pool, 0, 0);
	f->res_headers = pool->lend(pool, 0, 0);
	f->res_cookies = pool->lend(pool, 0, 0);
	f->res_body = pool->lend(pool, 0, 0);

	f->append_header = &fetcher_append_header;
	f->clear_headers = &fetcher_clear_headers;

	f->append_formpost_kv = &fetcher_append_formpost_kv;
	f->append_formpost_file = &fetcher_append_formpost_file;
	f->append_formpost_filebuf = &fetcher_append_formpost_filebuf;
	f->clear_formpost = &fetcher_clear_formpost;

	f->get = &fetcher_get;
	f->post = &fetcher_post;
	f->postform = &fetcher_postform;
	f->put = &fetcher_put;
	f->delete = &fetcher_delete;
	return f;
}

void fetcher_free(void *val)
{
	assert(val);
	fetcher_t *f = (fetcher_t*)val;
	if (f->priv)
		free_fetcher_inner(FI);
	if (f->req_url)
		buf_return(f->req_url);
	if (f->res_headers)
		buf_return(f->res_headers);
	if (f->res_cookies)
		buf_return(f->res_cookies);
	if (f->res_body)
		buf_return(f->res_body);
	free(f);
}
