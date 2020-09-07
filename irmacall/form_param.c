#include <ctype.h>
#include <string.h>
#include <assert.h>
#include "wrapper.h"
#include "misc.h"
#include "list.h"
#include "form_param.h"

#define FPLI	((fparamlist_inner_t*)fplist->priv)
#define FPPI	((fparamparser_inner_t*)parser->priv)

typedef struct {
	int local;
	buf_pool_t *pool;
} fparamparser_inner_t;

typedef struct {
	int count;
	int fcount;
	list_t *list;
	fparamparser_inner_t *parser;
} fparamlist_inner_t;

static form_param_t* form_param_new(buf_pool_t *pool)
{
	form_param_t *fp = xcalloc(1, sizeof(form_param_t));
	fp->name = pool->lend(pool, 0, 0);
	fp->filename = pool->lend(pool, 0, 0);
	fp->content_type = pool->lend(pool, 0, 0);
	fp->content = pool->lend(pool, 0, 0);
	return fp;
}

static void form_param_free(void *data)
{
	form_param_t *fp = (form_param_t*)data;
	buf_return(fp->name);
	buf_return(fp->filename);
	buf_return(fp->content_type);
	//buf_force_reset(fp->content);
	buf_return(fp->content);
	free(fp);
}

static int form_param_find(void *data, void *val)
{
	const char *key = (const char*)val;
	form_param_t *fp = (form_param_t*)data;
	return (fp->name && !strcasecmp(fp->name->data, key));
}

static int form_param_find0(void *data, void *val)
{
	int *i = (int*)val;
	form_param_t *fp = (form_param_t*)data;
	return (fp->name && fp->filename->offset <= 0 && (*i)-- == 0);
}

static int form_param_find1(void *data, void *val)
{
	int *i = (int*)val;
	form_param_t *fp = (form_param_t*)data;
	return (fp->name && fp->filename->offset > 0 && (*i)-- == 0);
}

static int form_param_find2(void *data, void *val)
{
	const char *key = (const char*)val;
	form_param_t *fp = (form_param_t*)data;
	return (fp->name && fp->filename->offset <= 0 && !strcasecmp(fp->name->data, key));
}

static int form_param_find3(void *data, void *val)
{
	const char *key = (const char*)val;
	form_param_t *fp = (form_param_t*)data;
	return (fp->name && fp->filename->offset > 0 && !strcasecmp(fp->name->data, key));
}

/*
 * methods of list
 */
static void fparamlist_reset(fparamlist_t *fplist)
{
	assert(fplist);
	if (FPLI->count + FPLI->fcount > 0) {
		list_free(FPLI->list);
		FPLI->list = list_new(&form_param_free);
		FPLI->count = 0;
		FPLI->fcount = 0;
	}
}

static int fparamlist_count(fparamlist_t *fplist)
{
	assert(fplist);
	return FPLI->count + FPLI->fcount;
}

static int fparamlist_post_count(fparamlist_t *fplist)
{
	assert(fplist);
	return FPLI->count;
}

static int fparamlist_file_count(fparamlist_t *fplist)
{
	assert(fplist);
	return FPLI->fcount;
}

static form_param_t* fparamlist_get(fparamlist_t *fplist, int i)
{
	assert(fplist);
	if (i < 0 || i >= (FPLI->count + FPLI->fcount))
		return NULL;
	litem_t *li = FPLI->list->index(FPLI->list, i);
	return li ? li->data : NULL;
}

static form_param_t* fparamlist_get_post(fparamlist_t *fplist, int i)
{
	assert(fplist);
	if (i < 0 || i >= FPLI->count)
		return NULL;
	litem_t *li = FPLI->list->find(FPLI->list, &form_param_find0, (void*)&i);
	return li ? li->data : NULL;
}

static form_param_t* fparamlist_get_file(fparamlist_t *fplist, int i)
{
	assert(fplist);
	if (i < 0 || i >= FPLI->fcount)
		return NULL;
	litem_t *li = FPLI->list->find(FPLI->list, &form_param_find1, (void*)&i);
	return li ? li->data : NULL;
}

static form_param_t* fparamlist_find(fparamlist_t *fplist, const char *key)
{
	assert(fplist);
	if (!key)
		return NULL;
	litem_t *li = FPLI->list->find(FPLI->list, &form_param_find, (void*)key);
	return li ? li->data : NULL;
}

static form_param_t* fparamlist_find_post(fparamlist_t *fplist, const char *key)
{
	assert(fplist);
	if (!key)
		return NULL;
	litem_t *li = FPLI->list->find(FPLI->list, &form_param_find2, (void*)key);
	return li ? li->data : NULL;
}

static form_param_t* fparamlist_find_file(fparamlist_t *fplist, const char *key)
{
	assert(fplist);
	if (!key)
		return NULL;
	litem_t *li = FPLI->list->find(FPLI->list, &form_param_find3, (void*)key);
	return li ? li->data : NULL;
}

static form_param_t* fparamlist_ext(fparamlist_t *fplist)
{
	assert(fplist);
	form_param_t *fp = form_param_new(FPLI->parser->pool);
	FPLI->list->put(FPLI->list, fp, 0L);
	return fp;
}

/*
 * methods of parser
 */
static fparamlist_t* fparamlist_new(fparamparser_t *parser)
{
	assert(parser);
	fparamlist_t *fplist = xcalloc(1, sizeof(fparamlist_t));
	fplist->priv = xcalloc(1, sizeof(fparamlist_inner_t));
	FPLI->list = list_new(&form_param_free);
	FPLI->parser = parser->priv;
	fplist->reset = &fparamlist_reset;
	fplist->count = &fparamlist_count;
	fplist->post_count = &fparamlist_post_count;
	fplist->file_count = &fparamlist_file_count;
	fplist->get = &fparamlist_get;
	fplist->get_post = &fparamlist_get_post;
	fplist->get_file = &fparamlist_get_file;
	fplist->find = &fparamlist_find;
	fplist->find_post = &fparamlist_find_post;
	fplist->find_file = &fparamlist_find_file;
	fplist->ext = &fparamlist_ext;
	return fplist;
}

static void fparamlist_free(fparamlist_t *fplist)
{
	assert(fplist);
	list_free(FPLI->list);
	free(fplist->priv);
	free(fplist);
}

static int parse(fparamlist_t *fplist, buf_t *body, const char *boundary, int blen, int *fcount)
{
	/*
	 BODY SAMPLE:

	 POST /?name=Tom&age=100&nation=USA&favorite=cat&kid=3&friend=Jack HTTP/1.1
	 ...
	 Content-Type: multipart/form-data; boundary=------------------------65169b68e51336cb

	 --------------------------65169b68e51336cb
	 Content-Disposition: form-data; name="aaa"

	 111
	 --------------------------65169b68e51336cb
	 Content-Disposition: form-data; name="file1"; filename="1.log"
	 Content-Type: application/octet-stream

	 fenkey
	 --------------------------65169b68e51336cb--
	 */

	assert(fplist);
	fplist->reset(fplist);

	if (body->offset <= 0 || !boundary)
		return 0;

	if (blen <= 0)
		blen = strlen(boundary);

	const char *p = memstr(body->data, body->offset, boundary, blen);
	while (p && (p - body->data < body->offset)) {
		p += blen;
		const char *rnrn = memstr(p, body->offset - (p - body->data), "\r\n\r\n", 4);
		if (!rnrn)
			break;
		p = memcasestr(p, body->offset - (p - body->data), "Content-Disposition: ", 21);
		if (!p || p > rnrn)
			break;
		p += 21;

		/* Get name. */
		const char *q1 = memcasestr(p, body->offset - (p - body->data), " name=\"", 7);
		if (!q1 || q1 > rnrn)
			break;
		q1 += 7;
		const char *q2 = memchr(q1, (int)'"', body->offset - (q1 - body->data));
		if (!q2 || q2 <= q1 || q2 > rnrn)
			break;
		form_param_t *fp = fplist->ext(fplist);
		buf_append(fp->name, q1, q2 - q1);
		p = q2;

		int isfile = (q1 = memcasestr(p, body->offset - (p - body->data), " filename=\"", 11)) && (q1 < rnrn) ? 1 : 0;
		if (isfile) {
			/* Get filename. */
			q1 += 11;
			q2 = memchr(q1, (int)'"', body->offset - (q1 - body->data));
			if (!q2 || q2 <= q1 || q2 > rnrn)
				goto __exception;
			buf_append(fp->filename, q1, q2 - q1);

			/* Get Content-Type. */
			p = q2 + 1;
			q1 = memcasestr(p, body->offset - (p - body->data), "Content-Type: ", 14);
			if (!q1 || q1 > rnrn)
				goto __exception;
			p = q1 + 14;
			if (q1)
				buf_append(fp->content_type, p, rnrn - p);
			p = rnrn + 4;
			FPLI->fcount++;
		} else {
			p = rnrn + 4;
			FPLI->count++;
		}

		/* Get Content. */
		q1 = memstr(p, body->offset - (p - body->data), boundary, blen);
		if (!q1 || q1 - p < 4)
			goto __exception;
		buf_append(fp->content, p, q1 - 4 - p);
		p = q1;
		continue;

	__exception:
		buf_printf(fp->content, "(invalid)");
		break;
	}
	if (fcount)
		*fcount = FPLI->fcount;
	return (FPLI->count + FPLI->fcount);
}

fparamparser_t* fparamparser_new(buf_pool_t *pool)
{
	fparamparser_t *parser = xmalloc(sizeof(fparamparser_t));
	parser->priv = xcalloc(1, sizeof(fparamparser_inner_t));
	FPPI->local = !pool;
	FPPI->pool = pool ? pool : buf_pool_new();
	parser->parse = &parse;
	parser->fparamlist_new = &fparamlist_new;
	parser->fparamlist_free = &fparamlist_free;
	return parser;
}

void fparamparser_free(fparamparser_t *parser, int release_pool)
{
	assert(parser && FPPI->pool);
	if (FPPI->local || release_pool)
		buf_pool_free(FPPI->pool);
	free(parser->priv);
	free(parser);
}
