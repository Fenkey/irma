#include "icall.h"

static void handle_unlock()
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;
	app->handle_unlock(w);
}

static MonoObject* get_global_object()
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;
	return app->obj_global;
}

static void once_over()
{
	worker_t *w = CURRENT;
	w->once_over(w);
}

static int fuse_check(MonoString *handler)
{
	worker_t *w = CURRENT;
	char *h = mono_string_to_utf8(handler);
	if (!h)
		return 0;
	int ret = w->fuse_check(w, h);
	mono_free(h);
	return ret < 0 ? 0 : 1;
}

static int request_ismock()
{
	worker_t *w = CURRENT;
	return w->request_ismock(w);
}

static int request_accept()
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;

	int ret = w->request_accept(w);
	app->keepalive(w);
	return ret;
}

static MonoString* get_request_method()
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;

	const char *p = w->request_method(w);
	return mono_string_new(app->domain, p ? p : "");
}

static MonoString* get_request_uri()
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;

	const char *p = w->request_uri(w);
	return mono_string_new(app->domain, p ? p : "");
}

static MonoString* get_request_querystring()
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;

	const char *p = w->request_querystring(w);
	return mono_string_new(app->domain, p ? p : "");
}

static MonoString* get_request_contenttype()
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;

	const char *p = w->request_contenttype(w);
	return mono_string_new(app->domain, p ? p : "");
}

static MonoString* get_all_request_headers()
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;

	buf_reset(app->buf);

	char **p = w->fcgi_params(w);
	for (; *p; p++) {
		if (strncasecmp(*p, "HTTP_", 5))
			continue;
		buf_printf_ext(app->buf, "%s\r\n", *p);
	}

	MonoString *ret = NULL;
	if (app->buf->offset < 2) {
		ret = mono_string_new(app->domain, "");
	} else {
		app->buf->offset -= 2;
		app->buf->data[app->buf->offset] = 0;
		ret = mono_string_new(app->domain, app->buf->data);
	}
	return ret;
}

static MonoString* get_fcgi_param(MonoString *param_name)
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;

	char *p = mono_string_to_utf8(param_name);
	if (!p)
		return NULL;
	const char *v = w->get_fcgi_param(w, p);
	mono_free(p);
	return mono_string_new(app->domain, v ? v : "");
}

static int get_request_get_params_count()
{
	worker_t *w = CURRENT;
	int count = w->request_get_parse(w);
	return count;
}

static MonoArray* get_request_get_param(int index, MonoString **param_name)
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;

	MonoArray *param_value = NULL;

	if (w->request_get_parse(w) <= index)
		goto __empty;

	int vlen;
	const char *v, *p = w->request_get_param_by_index(w, index, &v, &vlen);
	if (p && v) {
		*param_name = mono_string_new(app->domain, p);
		param_value = mono_array_new(app->domain, mono_get_byte_class(), vlen);
		int i = 0;
		for (; i < vlen; i++)
			mono_array_set(param_value, unsigned char, i, ((unsigned char*)v)[i]);
	} else {
__empty:
		*param_name = NULL;
	}
	return param_value;
}

static int get_request_post_params_count()
{
	worker_t *w = CURRENT;
	int file_count = 0, count = w->request_post_parse(w, &file_count);
	count -= file_count;
	return count > 0 ? count : 0;
}

static MonoArray* get_request_post_param(int index, MonoString **param_name)
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;

	MonoArray *param_value = NULL;

	int file_count = 0, count = w->request_post_parse(w, &file_count);
	count -= file_count;
	if (count <= 0 || count <= index)
		goto __empty;

	unsigned int vlen;
	const char *v, *p = w->request_generic_post_param_by_index(w, index, &v, &vlen);
	if (p && v) {
		*param_name = mono_string_new(app->domain, p);
		param_value = mono_array_new(app->domain, mono_get_byte_class(), vlen);
		int i = 0;
		for (; i < vlen; i++)
			mono_array_set(param_value, unsigned char, i, ((unsigned char*)v)[i]);
	} else {
__empty:
		*param_name = NULL;
	}
	return param_value;
}

static int get_request_file_params_count()
{
	worker_t *w = CURRENT;
	int file_count = 0, count = w->request_post_parse(w, &file_count);
	return count > 0 ? file_count : 0;
}

static MonoArray* get_request_file_param(int index, MonoString **param_name, MonoString **file_name, MonoString **content_type)
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;

	MonoArray *param_value = NULL;

	int file_count = 0, count = w->request_post_parse(w, &file_count);
	if (count <= 0 || file_count <= index)
		goto __empty;

	unsigned int vlen;
	const char *v, *fname, *ctype, *p = w->request_file_post_param_by_index(w, index, &v, &vlen, &fname, &ctype);
	if (p && v && vlen > 0 && ctype) {
		*param_name = mono_string_new(app->domain, p);
		*file_name = mono_string_new(app->domain, fname);
		*content_type = mono_string_new(app->domain, ctype);
		param_value = mono_array_new(app->domain, mono_get_byte_class(), vlen);
		int i = 0;
		for (; i < vlen; i++)
			mono_array_set(param_value, unsigned char, i, ((unsigned char*)v)[i]);
	} else {
__empty:
		*param_name = NULL;
		*file_name = NULL;
		*content_type = NULL;
	}
	return param_value;
}

static MonoArray* get_request_body()
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;

	MonoArray *body;
	unsigned int blen = 0;
	const char *p = w->request_body(w, &blen, NULL);
	if (p && blen > 0) {
		body = mono_array_new(app->domain, mono_get_byte_class(), blen);
		int i = 0;
		for (; i < blen; i++)
			mono_array_set(body, unsigned char, i, ((unsigned char*)p)[i]);
	} else {
		body = mono_array_new(app->domain, mono_get_byte_class(), 0);
	}
	return body;
}

static MonoArray* request_dump()
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;

	MonoArray *dump;
	buf_t *d = w->request_dump(w);
	if (d) {
		dump = mono_array_new(app->domain, mono_get_byte_class(), d->offset);
		int i = 0;
		for (; i < d->offset; i++)
			mono_array_set(dump, unsigned char, i, ((unsigned char*)d->data)[i]);
		buf_force_reset(d);
		buf_return(d);
	} else
		dump = mono_array_new(app->domain, mono_get_byte_class(), 0);
	return dump;
}

static void add_response_header(MonoString *key, MonoString *value)
{
	if (key && value) {
		char *k = mono_string_to_utf8(key);
		char *v = mono_string_to_utf8(value);
		worker_t *w = CURRENT;
		w->response_add_header(w, k, v);
		mono_free(k);
		mono_free(v);
	}
}

static void clear_response_headers()
{
	worker_t *w = CURRENT;
	w->response_clear_headers(w);
}

static void send_header()
{
	worker_t *w = CURRENT;
	w->send_header(w);
}

static void __send(MonoArray *content)
{
	if (!content)
		return;
	int len = mono_array_length(content);
	if (len <= 0)
		return;
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;
	mono_array_copy(content, len, app->buf);
	w->send(w, app->buf);
}

static void send_http(int rescode, MonoArray *content)
{
	worker_t *w = CURRENT;
	buf_t *buf = NULL;
	if (content) {
		int len = mono_array_length(content);
		if (len > 0) {
			app_t *app = (app_t*)w->priv_app;
			mono_array_copy(content, len, app->buf);
			buf = app->buf;
		}
	}
	w->send_http(w, rescode, buf);
}

static void echo(MonoArray *content)
{
	if (content) {
		int len = mono_array_length(content);
		if (len > 0) {
			worker_t *w = CURRENT;
			app_t *app = (app_t*)w->priv_app;
			mono_array_copy(content, len, app->buf);
			w->echo(w, app->buf);
		}
	}
}

static void redirect(MonoString *location)
{
	char *p = mono_string_to_utf8(location);
	if (p) {
		worker_t *w = CURRENT;
		w->redirect(w, p);
		mono_free(p);
	}
}

static icall_item_t __items[] = {
	ICALL_ITEM(HandleUnlock, handle_unlock),
	ICALL_ITEM(GetGlobalObject, get_global_object),
	ICALL_ITEM(OnceOver, once_over),
	ICALL_ITEM(FuseCheck, fuse_check),
	ICALL_ITEM(RequestIsMock, request_ismock),
	ICALL_ITEM(RequestAccept, request_accept),
	ICALL_ITEM(GetRequestMethod, get_request_method),
	ICALL_ITEM(GetRequestUri, get_request_uri),
	ICALL_ITEM(GetRequestQueryString, get_request_querystring),
	ICALL_ITEM(GetRequestContentType, get_request_contenttype),
	ICALL_ITEM(GetAllRequestHeaders, get_all_request_headers),
	ICALL_ITEM(GetRequestParam, get_fcgi_param),
	ICALL_ITEM(GetRequestGetParamsCount, get_request_get_params_count),
	ICALL_ITEM(GetRequestGetParam, get_request_get_param),
	ICALL_ITEM(GetRequestPostParamsCount, get_request_post_params_count),
	ICALL_ITEM(GetRequestPostParam, get_request_post_param),
	ICALL_ITEM(GetRequestFileParamsCount, get_request_file_params_count),
	ICALL_ITEM(GetRequestFileParam, get_request_file_param),
	ICALL_ITEM(GetRequestBody, get_request_body),
	ICALL_ITEM(RequestDump, request_dump),
	ICALL_ITEM(AddResponseHeader, add_response_header),
	ICALL_ITEM(ClearResponseHeaders, clear_response_headers),
	ICALL_ITEM(SendHeader, send_header),
	ICALL_ITEM(Send, __send),
	ICALL_ITEM(SendHttp, send_http),
	ICALL_ITEM(Echo, echo),
	ICALL_ITEM(Redirect, redirect),
	ICALL_ITEM_NULL
};

void reg_fcgi() { regit(__items); }
