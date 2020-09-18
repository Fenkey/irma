#include "icall.h"

static int append_header(MonoString *header)
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;
	fetcher_t *f = app->fetcher;

	char *p = mono_string_to_utf8(header);
	if (!p)
		return -1;
	int ret = f->append_header(f, p);
	mono_free(p);
	return ret;
}

static int clear_headers()
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;
	fetcher_t *f = app->fetcher;
	return f->clear_headers(f);
}

static int append_formpost_kv(MonoString *key, MonoString *value)
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;
	fetcher_t *f = app->fetcher;

	char *k = mono_string_to_utf8(key);
	char *v = mono_string_to_utf8(value);
	int ret = f->append_formpost_kv(f, k, v);
	if (k) mono_free(k);
	if (v) mono_free(v);
	return ret;
}

static int append_formpost_file(MonoString *name, MonoString *file, MonoString *content_type)
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;
	fetcher_t *f = app->fetcher;

	char *n = mono_string_to_utf8(name);
	char *pf = mono_string_to_utf8(file);
	char *ct = mono_string_to_utf8(content_type);

	int ret = f->append_formpost_file(f, n, pf, ct);
	if (n) mono_free(n);
	if (pf) mono_free(pf);
	if (ct) mono_free(ct);
	return ret;
}

static int append_formpost_filebuf(MonoString *name, MonoString *file, MonoArray *body, MonoString *content_type)
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;
	fetcher_t *f = app->fetcher;

	char *n = mono_string_to_utf8(name);
	char *pf = mono_string_to_utf8(file);
	char *ct = mono_string_to_utf8(content_type);

	int len = body ? mono_array_length(body) : 0;
	if (len > 0)
		mono_array_copy(body, len, app->buf);
	int ret = f->append_formpost_filebuf(f, n, pf, app->buf->data, app->buf->offset, ct);
	buf_force_reset(app->buf);
	if (n) mono_free(n);
	if (pf) mono_free(pf);
	if (ct) mono_free(ct);
	return ret;
}

static int clear_formpost()
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;
	fetcher_t *f = app->fetcher;
	return f->clear_formpost(f);
}

static long get(MonoString *url, int timeout)
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;
	fetcher_t *f = app->fetcher;

	char *u = mono_string_to_utf8(url);
	if (!u)
		return -1;
	long code = f->get(f, u, timeout);
	mono_free(u);
	return code;
}

static long post(MonoString *url, MonoArray *body, int timeout)
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;
	fetcher_t *f = app->fetcher;

	char *u = mono_string_to_utf8(url);
	if (!u)
		return -1;
	int len = body ? mono_array_length(body) : 0;
	if (len > 0)
		mono_array_copy(body, len, app->buf);
	long code = f->post(f, u, app->buf->data, app->buf->offset, timeout);
	//buf_force_reset(app->buf);
	mono_free(u);
	return code;
}

static long postform(MonoString *url, int timeout)
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;
	fetcher_t *f = app->fetcher;

	char *u = mono_string_to_utf8(url);
	if (!u)
		return -1;
	long code = f->postform(f, u, timeout);
	mono_free(u);
	return code;
}

static long put(MonoString *url, MonoArray *body, int timeout)
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;
	fetcher_t *f = app->fetcher;

	char *u = mono_string_to_utf8(url);
	if (!u)
		return -1;
	int len = body ? mono_array_length(body) : 0;
	if (len > 0)
		mono_array_copy(body, len, app->buf);
	long code = f->put(f, u, app->buf->data, app->buf->offset, timeout);
	//buf_force_reset(app->buf);
	mono_free(u);
	return code;
}

static long delete(MonoString *url, int timeout)
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;
	fetcher_t *f = app->fetcher;

	char *u = mono_string_to_utf8(url);
	if (!u)
		return -1;
	long code = f->delete(f, u, timeout);
	mono_free(u);
	return code;
}

static MonoArray* res_body()
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;
	fetcher_t *f = app->fetcher;

	MonoArray *body = mono_array_new(app->domain, mono_get_byte_class(), f->res_body->offset);
	int i = 0;
	for (; i < f->res_body->offset; i++)
		mono_array_set(body, unsigned char, i, ((unsigned char*)f->res_body->data)[i]);
	return body;
}

static MonoString* res_headers()
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;
	fetcher_t *f = app->fetcher;
	return f->res_headers->offset > 0 ? \
		mono_string_new(app->domain, f->res_headers->data) : mono_string_new(app->domain, "");
}

static MonoString* error()
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;
	fetcher_t *f = app->fetcher;
	return f->error[0] ? mono_string_new(app->domain, f->error) : NULL;
}

static double timeused()
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;
	fetcher_t *f = app->fetcher;
	return f->time_used;
}

static icall_item_t __items[] = {
	ICALL_ITEM(FetcherAppendHeader, append_header),
	ICALL_ITEM(FetcherClearHeaders, clear_headers),
	ICALL_ITEM(FetcherAppendFormPostKv, append_formpost_kv),
	ICALL_ITEM(FetcherAppendFormPostFile, append_formpost_file),
	ICALL_ITEM(FetcherAppendFormPostFileBuf, append_formpost_filebuf),
	ICALL_ITEM(FetcherClearFormPost, clear_formpost),
	ICALL_ITEM(FetcherGet, get),
	ICALL_ITEM(FetcherPost, post),
	ICALL_ITEM(FetcherPostForm, postform),
	ICALL_ITEM(FetcherPut, put),
	ICALL_ITEM(FetcherDelete, delete),
	ICALL_ITEM(FetcherResBody, res_body),
	ICALL_ITEM(FetcherResHeaders, res_headers),
	ICALL_ITEM(FetcherError, error),
	ICALL_ITEM(FetcherTimeUsed, timeused),
	ICALL_ITEM_NULL
};

void reg_fetcher() { regit(__items); }
