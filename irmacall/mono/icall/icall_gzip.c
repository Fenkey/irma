#include "icall.h"

static MonoArray* __gzip(MonoArray *data)
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;

	int len = mono_array_length(data);
	if (len <= 0)
		return mono_array_new(app->domain, mono_get_byte_class(), 0);

	buf_data_reset(app->buf, len);
	int i = 0;
	unsigned char uc;
	for (; i < len; i++) {
		uc = mono_array_get(data, unsigned char, i);
		buf_append(app->buf, (const char*)&uc, 1);
	}

	len = 0;
	buf_t *output = w->pool->lend(w->pool, 0, 0);
	if (gzip(app->buf->data, app->buf->offset, output))
		len = output->offset;
	MonoArray *ret = mono_array_new(app->domain, mono_get_byte_class(), len);
	for (i = 0; i < len; i++)
		mono_array_set(ret, unsigned char, i, ((unsigned char*)output->data)[i]);

	buf_force_reset(app->buf);
	buf_force_reset(output);
	buf_return(output);
	return ret;
}

static MonoArray* __gunzip(MonoArray *data)
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;

	int len = mono_array_length(data);
	if (len <= 0)
		return mono_array_new(app->domain, mono_get_byte_class(), 0);

	buf_data_reset(app->buf, len);
	int i = 0;
	unsigned char uc;
	for (; i < len; i++) {
		uc = mono_array_get(data, unsigned char, i);
		buf_append(app->buf, (const char*)&uc, 1);
	}

	len = 0;
	buf_t *output = w->pool->lend(w->pool, 0, 0);
	if (gunzip(app->buf->data, app->buf->offset, output))
		len = output->offset;
	MonoArray *ret = mono_array_new(app->domain, mono_get_byte_class(), len);
	for (i = 0; i < len; i++)
		mono_array_set(ret, unsigned char, i, ((unsigned char*)output->data)[i]);

	buf_force_reset(app->buf);
	buf_force_reset(output);
	buf_return(output);
	return ret;
}

static icall_item_t __items[] = {
	ICALL_ITEM(GZip, __gzip),
	ICALL_ITEM(GUnZip, __gunzip),
	ICALL_ITEM_NULL
};

void reg_gzip() { regit(__items); }
