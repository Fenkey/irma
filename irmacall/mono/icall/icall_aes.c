#include "icall.h"

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

	buf_printf(app->buf, "aes-ecb-enc-key-%s", k);
	AES_KEY *ak = (AES_KEY*)app->map->get(app->map, app->buf->data);
	if (!ak) {
		ak = aes_encrypt_key(k, strlen(k));
		if (!ak) {
			mono_free(k);
			return mono_array_new(app->domain, mono_get_byte_class(), 0);
		}
		app->map->set(app->map, app->buf->data, ak, &aes_key_free);
	}
	mono_free(k);

	int nlen = len;
	int plen = aes_plen(ptype, &nlen);
	//DEBUG(w->log, "Aes ecb encrypte: len=%d, plen==%d, nlen=%d", len, plen, nlen);

	mono_array_in_b(content, len, app->buf, nlen);
	if (plen > 0) {
		memset(app->buf->data + len, ptype ? plen : 0, plen);
		app->buf->offset = nlen;
	}

	buf_t *output = w->pool->lend(w->pool, nlen, 0);
	output->offset = nlen;

	int i = 0;
	for (; i < nlen; i += AES_BLOCK_SIZE)
		aes_ecb_encrypt(ak, app->buf->data + i, output->data + i);

	MonoArray *ret = mono_array_new(app->domain, mono_get_byte_class(), output->offset);
	mono_array_out_b(ret, output);
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
	if (len < AES_BLOCK_SIZE || (len % AES_BLOCK_SIZE)) {
		mono_free(k);
		return mono_array_new(app->domain, mono_get_byte_class(), 0);
	}

	buf_printf(app->buf, "aes-ecb-dec-key-%s", k);
	AES_KEY *ak = (AES_KEY*)app->map->get(app->map, app->buf->data);
	if (!ak) {
		ak = aes_decrypt_key(k, strlen(k));
		if (!ak) {
			mono_free(k);
			return mono_array_new(app->domain, mono_get_byte_class(), 0);
		}
		app->map->set(app->map, app->buf->data, ak, &aes_key_free);
	}
	mono_free(k);

	mono_array_in(content, len, app->buf);

	int i = 0;
	char *dec = xcalloc(len, sizeof(char));
	for (; i < len; i += AES_BLOCK_SIZE)
		aes_ecb_decrypt(ak, app->buf->data + i, dec + i);

	if (ptype) {
		int plen = dec[len - 1];
		//DEBUG(w->log, "Aes ecb decrypt: len=%d, plen==%d", len, plen);
		len -= plen;
	}

	MonoArray *ret = NULL;
	/* In typical cases, len may be <=0 */
	if (len > 0) {
		ret = mono_array_new(app->domain, mono_get_byte_class(), len);
		mono_array_out(ret, dec, len);
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

	buf_printf(app->buf, "aes-cbc-enc-key-%s", k);
	AES_KEY *ak = (AES_KEY*)app->map->get(app->map, app->buf->data);
	if (!ak) {
		ak = aes_encrypt_key(k, strlen(k));
		if (!ak) {
			mono_free(k);
			mono_free(v);
			return mono_array_new(app->domain, mono_get_byte_class(), 0);
		}
		app->map->set(app->map, app->buf->data, ak, &aes_key_free);
	}
	mono_free(k);

	int nlen = len;
	int plen = aes_plen(ptype, &nlen);
	//DEBUG(w->log, "Aes cbc encrypte: len=%d, plen==%d, nlen=%d", len, plen, nlen);

	mono_array_in_b(content, len, app->buf, nlen);
	if (plen > 0) {
		memset(app->buf->data + len, ptype ? plen : 0, plen);
		app->buf->offset = nlen;
	}

	buf_t *output = w->pool->lend(w->pool, nlen, 0);
	output->offset = nlen;

	aes_cbc_encrypt(ak, v, app->buf->data, app->buf->offset, output->data);
	mono_free(v);

	MonoArray *ret = mono_array_new(app->domain, mono_get_byte_class(), output->offset);
	mono_array_out_b(ret, output);
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
	if (len < AES_BLOCK_SIZE || (len % AES_BLOCK_SIZE)) {
		mono_free(k);
		mono_free(v);
		return mono_array_new(app->domain, mono_get_byte_class(), 0);
	}

	buf_printf(app->buf, "aes-cbc-dec-key-%s", k);
	AES_KEY *ak = (AES_KEY*)app->map->get(app->map, app->buf->data);
	if (!ak) {
		ak = aes_decrypt_key(k, strlen(k));
		if (!ak) {
			mono_free(k);
			mono_free(v);
			return mono_array_new(app->domain, mono_get_byte_class(), 0);
		}
		app->map->set(app->map, app->buf->data, ak, &aes_key_free);
	}
	mono_free(k);

	mono_array_in(content, len, app->buf);

	char *dec = xcalloc(len, sizeof(char));
	aes_cbc_decrypt(ak, v, app->buf->data, len, dec);
	mono_free(v);

	if (ptype) {
		int plen = dec[len - 1];
		//DEBUG(w->log, "Aes cbc decrypt: len=%d, plen==%d", len, plen);
		len -= plen;
	}

	MonoArray *ret = NULL;
	/* In typical cases, len may be <=0 */
	if (len > 0) {
		ret = mono_array_new(app->domain, mono_get_byte_class(), len);
		mono_array_out(ret, dec, len);
	}
	free(dec);
	//buf_force_reset(app->buf);
	return ret;
}

static icall_item_t __items[] = {
	ICALL_ITEM(AesCbcEncrypt, __cbc_encrypt),
	ICALL_ITEM(AesCbcDecrypt, __cbc_decrypt),
	ICALL_ITEM(AesEcbEncrypt, __ecb_encrypt),
	ICALL_ITEM(AesEcbDecrypt, __ecb_decrypt),
	ICALL_ITEM_NULL
};

void reg_aes() { regit(__items); }
