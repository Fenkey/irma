#include "icall.h"
#include "../../log.h"

/*
 *****************************
 * ECB
 *****************************
 */
static MonoArray* __ecb_encrypt(MonoString *key, MonoArray *content, int ptype)
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;

	char *k = mono_string_to_utf8(key);
	if (!k)
		return NULL;

	int len = mono_array_length(content);
	if (len <= 0) {
		mono_free(k);
		return mono_array_new(app->domain, mono_get_byte_class(), 0);
	}

	buf_printf(app->buf, "des-ecb-enc-key-%s", k);
	DES_KEY *dk = (DES_KEY*)app->map->get(app->map, app->buf->data);
	if (!dk) {
		dk = des_key_new(k, 1);
		if (!dk) {
			mono_free(k);
			return mono_array_new(app->domain, mono_get_byte_class(), 0);
		}
		app->map->set(app->map, app->buf->data, dk, &des_key_free);
	}
	mono_free(k);

	int nlen = len;
	int plen = des_plen(ptype, &nlen);
	//DEBUG(w->log, "Des ecb encrypte: len=%d, plen==%d, nlen=%d", len, plen, nlen);

	buf_data_reset(app->buf, nlen);
	int i = 0;
	unsigned char uc;
	for (; i < len; i++) {
		uc = mono_array_get(content, unsigned char, i);
		buf_append(app->buf, (const char*)&uc, 1);
	}
	if (plen > 0)
		memset(app->buf->data + len, ptype ? plen : 0, plen);
	app->buf->offset = nlen;

	buf_t *output = w->pool->lend(w->pool, nlen, 0);
	output->offset = nlen;

	for (i = 0; i < nlen; i += DES_BLOCK_SIZE)
		wdes_ecb_encrypt(dk, app->buf->data + i, output->data + i);

	MonoArray *ret = mono_array_new(app->domain, mono_get_byte_class(), output->offset);
	for (i = 0; i < output->offset; i++)
		mono_array_set(ret, unsigned char, i, ((unsigned char*)output->data)[i]);
	buf_force_reset(output);
	buf_return(output);
	return ret;
}

static MonoArray* __ecb_decrypt(MonoString *key, MonoArray *content, int ptype)
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;

	char *k = mono_string_to_utf8(key);
	if (!k)
		return NULL;

	int len = mono_array_length(content);
	if (len < DES_BLOCK_SIZE || (len % DES_BLOCK_SIZE)) {
		mono_free(k);
		return mono_array_new(app->domain, mono_get_byte_class(), 0);
	}

	buf_printf(app->buf, "des-ecb-dec-key-%s", k);
	DES_KEY *dk = (DES_KEY*)app->map->get(app->map, app->buf->data);
	if (!dk) {
		dk = des_key_new(k, 1);
		if (!dk) {
			mono_free(k);
			return mono_array_new(app->domain, mono_get_byte_class(), 0);
		}
		app->map->set(app->map, app->buf->data, dk, &des_key_free);
	}
	mono_free(k);

	buf_data_reset(app->buf, len);
	int i = 0;
	unsigned char uc;
	for (; i < len; i++) {
		uc = mono_array_get(content, unsigned char, i);
		buf_append(app->buf, (const char*)&uc, 1);
	}

	char *dec = xcalloc(len, sizeof(char));
	for (i = 0; i < len; i += DES_BLOCK_SIZE)
		wdes_ecb_decrypt(dk, app->buf->data + i, dec + i);

	if (ptype) {
		int plen = dec[len - 1];
		//DEBUG(w->log, "Des ecb decrypt: len=%d, plen==%d", len, plen);
		len -= plen;
	}

	MonoArray *ret = NULL;
	/* In typical cases, len may be <=0 */
	if (len > 0) {
		ret = mono_array_new(app->domain, mono_get_byte_class(), len);
		for (i = 0; i < len; i++)
			mono_array_set(ret, unsigned char, i, ((unsigned char*)dec)[i]);
	}
	free(dec);
	//buf_force_reset(app->buf);
	return ret;
}

/*
 *****************************
 * CBC
 *****************************
 */
static MonoArray* __cbc_encrypt(MonoString *key, MonoString *iv, MonoArray *content, int ptype)
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;

	char *k = mono_string_to_utf8(key);
	if (!k)
		return NULL;

	char *v = mono_string_to_utf8(iv);
	if (!v) {
		mono_free(k);
		return NULL;
	}

	int len = mono_array_length(content);
	if (len <= 0) {
		mono_free(k);
		mono_free(v);
		return mono_array_new(app->domain, mono_get_byte_class(), 0);
	}

	buf_printf(app->buf, "des-cbc-enc-key-%s", k);
	DES_KEY *dk = (DES_KEY*)app->map->get(app->map, app->buf->data);
	if (!dk) {
		dk = des_key_new(k, 1);
		if (!dk) {
			mono_free(k);
			mono_free(v);
			return mono_array_new(app->domain, mono_get_byte_class(), 0);
		}
		app->map->set(app->map, app->buf->data, dk, &des_key_free);
	}
	mono_free(k);

	int nlen = len;
	int plen = des_plen(ptype, &nlen);
	//DEBUG(w->log, "Des cbc encrypte: len=%d, plen==%d, nlen=%d", len, plen, nlen);

	buf_data_reset(app->buf, nlen);
	int i = 0;
	unsigned char uc;
	for (; i < len; i++) {
		uc = mono_array_get(content, unsigned char, i);
		buf_append(app->buf, (const char*)&uc, 1);
	}
	if (plen > 0)
		memset(app->buf->data + len, ptype ? plen : 0, plen);
	app->buf->offset = nlen;

	buf_t *output = w->pool->lend(w->pool, nlen, 0);
	output->offset = nlen;

	wdes_cbc_encrypt(dk, v, app->buf->data, app->buf->offset, output->data);
	mono_free(v);

	MonoArray *ret = mono_array_new(app->domain, mono_get_byte_class(), output->offset);
	for (i = 0; i < output->offset; i++)
		mono_array_set(ret, unsigned char, i, ((unsigned char*)output->data)[i]);
	buf_force_reset(output);
	buf_return(output);
	return ret;
}

static MonoArray* __cbc_decrypt(MonoString *key, MonoString *iv, MonoArray *content, int ptype)
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;

	char *k = mono_string_to_utf8(key);
	if (!k)
		return NULL;

	char *v = mono_string_to_utf8(iv);
	if (!v) {
		mono_free(k);
		return NULL;
	}

	int len = mono_array_length(content);
	if (len < DES_BLOCK_SIZE || (len % DES_BLOCK_SIZE)) {
		mono_free(k);
		mono_free(v);
		return mono_array_new(app->domain, mono_get_byte_class(), 0);
	}

	buf_printf(app->buf, "des-cbc-dec-key-%s", k);
	DES_KEY *dk = (DES_KEY*)app->map->get(app->map, app->buf->data);
	if (!dk) {
		dk = des_key_new(k, 1);
		if (!dk) {
			mono_free(k);
			mono_free(v);
			return mono_array_new(app->domain, mono_get_byte_class(), 0);
		}
		app->map->set(app->map, app->buf->data, dk, &des_key_free);
	}
	mono_free(k);

	buf_data_reset(app->buf, len);
	int i = 0;
	unsigned char uc;
	for (; i < len; i++) {
		uc = mono_array_get(content, unsigned char, i);
		buf_append(app->buf, (const char*)&uc, 1);
	}

	char *dec = xcalloc(len, sizeof(char));
	wdes_cbc_decrypt(dk, v, app->buf->data, len, dec);
	mono_free(v);

	if (ptype) {
		int plen = dec[len - 1];
		//DEBUG(w->log, "Des cbc decrypt: len=%d, plen==%d", len, plen);
		len -= plen;
	}

	MonoArray *ret = NULL;
	/* In typical cases, len may be <=0 */
	if (len > 0) {
		ret = mono_array_new(app->domain, mono_get_byte_class(), len);
		for (i = 0; i < len; i++)
			mono_array_set(ret, unsigned char, i, ((unsigned char*)dec)[i]);
	}
	free(dec);
	//buf_force_reset(app->buf);
	return ret;
}

/*
 *****************************
 * NCBC
 *****************************
 */
static MonoArray* __ncbc_encrypt(MonoString *key, MonoString *iv, MonoArray *content, int ptype)
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;

	char *k = mono_string_to_utf8(key);
	if (!k)
		return NULL;

	char *v = mono_string_to_utf8(iv);
	if (!v) {
		mono_free(k);
		return NULL;
	}

	int len = mono_array_length(content);
	if (len <= 0) {
		mono_free(k);
		mono_free(v);
		return mono_array_new(app->domain, mono_get_byte_class(), 0);
	}

	buf_printf(app->buf, "des-ncbc-enc-key-%s", k);
	DES_KEY *dk = (DES_KEY*)app->map->get(app->map, app->buf->data);
	if (!dk) {
		dk = des_key_new(k, 1);
		if (!dk) {
			mono_free(k);
			mono_free(v);
			return mono_array_new(app->domain, mono_get_byte_class(), 0);
		}
		app->map->set(app->map, app->buf->data, dk, &des_key_free);
	}
	mono_free(k);

	int nlen = len;
	int plen = des_plen(ptype, &nlen);
	//DEBUG(w->log, "Des ncbc encrypte: len=%d, plen==%d, nlen=%d", len, plen, nlen);

	buf_data_reset(app->buf, nlen);
	int i = 0;
	unsigned char uc;
	for (; i < len; i++) {
		uc = mono_array_get(content, unsigned char, i);
		buf_append(app->buf, (const char*)&uc, 1);
	}
	if (plen > 0)
		memset(app->buf->data + len, ptype ? plen : 0, plen);
	app->buf->offset = nlen;

	buf_t *output = w->pool->lend(w->pool, nlen, 0);
	output->offset = nlen;

	wdes_ncbc_encrypt(dk, v, app->buf->data, app->buf->offset, output->data);
	mono_free(v);

	MonoArray *ret = mono_array_new(app->domain, mono_get_byte_class(), output->offset);
	for (i = 0; i < output->offset; i++)
		mono_array_set(ret, unsigned char, i, ((unsigned char*)output->data)[i]);
	buf_force_reset(output);
	buf_return(output);
	return ret;
}

static MonoArray* __ncbc_decrypt(MonoString *key, MonoString *iv, MonoArray *content, int ptype)
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;

	char *k = mono_string_to_utf8(key);
	if (!k)
		return NULL;

	char *v = mono_string_to_utf8(iv);
	if (!v) {
		mono_free(k);
		return NULL;
	}

	int len = mono_array_length(content);
	if (len < DES_BLOCK_SIZE || (len % DES_BLOCK_SIZE)) {
		mono_free(k);
		mono_free(v);
		return mono_array_new(app->domain, mono_get_byte_class(), 0);
	}

	buf_printf(app->buf, "des-ncbc-dec-key-%s", k);
	DES_KEY *dk = (DES_KEY*)app->map->get(app->map, app->buf->data);
	if (!dk) {
		dk = des_key_new(k, 1);
		if (!dk) {
			mono_free(k);
			mono_free(v);
			return mono_array_new(app->domain, mono_get_byte_class(), 0);
		}
		app->map->set(app->map, app->buf->data, dk, &des_key_free);
	}
	mono_free(k);

	buf_data_reset(app->buf, len);
	int i = 0;
	unsigned char uc;
	for (; i < len; i++) {
		uc = mono_array_get(content, unsigned char, i);
		buf_append(app->buf, (const char*)&uc, 1);
	}

	char *dec = xcalloc(len, sizeof(char));
	wdes_ncbc_decrypt(dk, v, app->buf->data, len, dec);
	mono_free(v);

	if (ptype) {
		int plen = dec[len - 1];
		//DEBUG(w->log, "Des ncbc decrypt: len=%d, plen==%d", len, plen);
		len -= plen;
	}

	MonoArray *ret = NULL;
	/* In typical cases, len may be <=0 */
	if (len > 0) {
		ret = mono_array_new(app->domain, mono_get_byte_class(), len);
		for (i = 0; i < len; i++)
			mono_array_set(ret, unsigned char, i, ((unsigned char*)dec)[i]);
	}
	free(dec);
	//buf_force_reset(app->buf);
	return ret;
}

static icall_item_t __items[] = {
	ICALL_ITEM(DesEcbEncrypt, __ecb_encrypt),
	ICALL_ITEM(DesEcbDecrypt, __ecb_decrypt),
	ICALL_ITEM(DesCbcEncrypt, __cbc_encrypt),
	ICALL_ITEM(DesCbcDecrypt, __cbc_decrypt),
	ICALL_ITEM(DesNCbcEncrypt, __ncbc_encrypt),
	ICALL_ITEM(DesNCbcDecrypt, __ncbc_decrypt),
	ICALL_ITEM_NULL
};

void reg_des() { regit(__items); }
