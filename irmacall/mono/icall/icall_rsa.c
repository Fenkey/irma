#include "icall.h"

static MonoArray* __encrypt(MonoString *keyfile, MonoString *keypwd, MonoString *content)
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;

	char *k = mono_string_to_utf8(keyfile);
	if (!k)
		return NULL;

	char *c = mono_string_to_utf8(content);
	if (!c) {
		mono_free(k);
		return mono_array_new(app->domain, mono_get_byte_class(), 0);
	}

	buf_printf(app->buf, "rsa-key-%s", k);
	RSA *rsa = (RSA*)app->map->get(app->map, app->buf->data);
	if (!rsa) {
		char *p = mono_string_to_utf8(keypwd);
		rsa = rsa_new_from_file(k, p);
		if (p)
			mono_free(p);
		if (!rsa) {
			mono_free(k);
			mono_free(c);
			return mono_array_new(app->domain, mono_get_byte_class(), 0);
		}
		app->map->set(app->map, app->buf->data, rsa, &rsa_free);
	}
	mono_free(k);

	buf_t *enc = app->buf;
	/* Note the mono_string_length(content) gets unicode length (e.g. "中国" is 2). */
	int len = rsa_encrypt(rsa, c, strlen(c), enc);
	MonoArray *ret = mono_array_new(app->domain, mono_get_byte_class(), len);
	mono_array_out(ret, enc->data, len);
	//buf_force_reset(enc);
	mono_free(c);
	return ret;
}

static MonoString* __decrypt(MonoString *keyfile, MonoString *keypwd, MonoArray *content)
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;

	char *k = mono_string_to_utf8(keyfile);
	if (!k)
		return NULL;

	int len = mono_array_length(content);
	if (len <= 0) {
		mono_free(k);
		return mono_string_new(app->domain, "");
	}

	MonoString *ret;
	buf_printf(app->buf, "rsa-key-%s", k);
	RSA *rsa = (RSA*)app->map->get(app->map, app->buf->data);
	if (!rsa) {
		char *p = mono_string_to_utf8(keypwd);
		rsa = rsa_new_from_file(k, p);
		if (p)
			mono_free(p);
		if (!rsa) {
			ret = mono_string_new(app->domain, "");
			mono_free(k);
			return ret;
		}
		app->map->set(app->map, app->buf->data, rsa, &rsa_free);
	}
	mono_free(k);

	mono_array_in(content, len, app->buf);

	buf_t *dec = w->pool->lend(w->pool, 0, 0);
	if (rsa_decrypt(rsa, app->buf->data, app->buf->offset, dec) > 0)
		ret = mono_string_new(app->domain, dec->data);
	else
		ret = mono_string_new(app->domain, "");
	//buf_force_reset(app->buf);
	buf_force_reset(dec);
	buf_return(dec);
	return ret;
}

static MonoArray* sign(int type, MonoString *keyfile, MonoString *keypwd, MonoString *content)
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;

	switch (type) {
	case 0:
		type = NID_md5;
		break;
	case 1:
		type = NID_sha1;
		break;
	case 2:
		type = NID_sha256;
		break;
	case 3:
		type = NID_sha512;
		break;
	default:
		return NULL;
	}

	char *k = mono_string_to_utf8(keyfile);
	if (!k)
		return NULL;

	char *c = mono_string_to_utf8(content);
	if (!c) {
		mono_free(k);
		return mono_array_new(app->domain, mono_get_byte_class(), 0);
	}

	buf_printf(app->buf, "rsa-key-%s", k);
	RSA *rsa = (RSA*)app->map->get(app->map, app->buf->data);
	if (!rsa) {
		char *p = mono_string_to_utf8(keypwd);
		rsa = rsa_new_from_file(k, p);
		if (p)
			mono_free(p);
		if (!rsa) {
			mono_free(k);
			mono_free(c);
			return mono_array_new(app->domain, mono_get_byte_class(), 0);
		}
		app->map->set(app->map, app->buf->data, rsa, &rsa_free);
	}
	mono_free(k);

	char sign[RSA_size(rsa) + 1];
	memset(sign, 0, sizeof(sign));
	int len = rsa_sign(rsa, type, c, strlen(c), sign);
	MonoArray *ret = mono_array_new(app->domain, mono_get_byte_class(), len);
	mono_array_out(ret, sign, len);
	mono_free(c);
	return ret;
}

static MonoBoolean verify(int type, MonoString *keyfile, MonoString *keypwd, MonoString *content, MonoArray *sign)
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;

	switch (type) {
	case 0:
		type = NID_md5;
		break;
	case 1:
		type = NID_sha1;
		break;
	case 2:
		type = NID_sha256;
		break;
	case 3:
		type = NID_sha512;
		break;
	default:
		return 0;
	}

	char *k = mono_string_to_utf8(keyfile);
	if (!k)
		return 0;

	char *c = mono_string_to_utf8(content);
	if (!c) {
		mono_free(k);
		return 0;
	}

	buf_printf(app->buf, "rsa-key-%s", k);
	RSA *rsa = (RSA*)app->map->get(app->map, app->buf->data);
	if (!rsa) {
		char *p = mono_string_to_utf8(keypwd);
		rsa = rsa_new_from_file(k, p);
		if (p)
			mono_free(p);
		if (!rsa) {
			mono_free(k);
			mono_free(c);
			return 0;
		}
		app->map->set(app->map, app->buf->data, rsa, &rsa_free);
	}
	mono_free(k);

	int len = mono_array_length(sign);
	if (len <= 0) {
		mono_free(c);
		return 0;
	}
	mono_array_in(sign, len, app->buf);
	int ret = rsa_verify(rsa, type, c, strlen(c), app->buf->data, app->buf->offset);
	//buf_force_reset(app->buf);
	mono_free(c);
	return ret;
}

static MonoArray* mem_encrypt(MonoString *key, MonoString *keypwd, MonoString *content)
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;

	char *k = mono_string_to_utf8(key);
	if (!k)
		return NULL;

	char *c = mono_string_to_utf8(content);
	if (!c) {
		mono_free(k);
		return mono_array_new(app->domain, mono_get_byte_class(), 0);
	}

	char *p = mono_string_to_utf8(keypwd);
	RSA *rsa = rsa_new_from_mem(k, p);
	if (p)
		mono_free(p);
	if (!rsa) {
		mono_free(k);
		mono_free(c);
		return mono_array_new(app->domain, mono_get_byte_class(), 0);
	}
	mono_free(k);

	buf_t *enc = app->buf;
	int len = rsa_encrypt(rsa, c, strlen(c), enc);
	MonoArray *ret = mono_array_new(app->domain, mono_get_byte_class(), len);
	mono_array_out(ret, enc->data, len);
	//buf_force_reset(enc);
	mono_free(c);
	rsa_free(rsa);
	return ret;
}

static MonoString* mem_decrypt(MonoString *key, MonoString *keypwd, MonoArray *content)
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;

	char *k = mono_string_to_utf8(key);
	if (!k)
		return NULL;

	int len = mono_array_length(content);
	if (len <= 0) {
		mono_free(k);
		return mono_string_new(app->domain, "");
	}

	MonoString *ret;
	char *p = mono_string_to_utf8(keypwd);
	RSA *rsa = rsa_new_from_mem(k, p);
	if (p)
		mono_free(p);
	if (!rsa) {
		ret = mono_string_new(app->domain, "");
		mono_free(k);
		return ret;
	}
	mono_free(k);

	mono_array_in(content, len, app->buf);

	buf_t *dec = w->pool->lend(w->pool, 0, 0);
	if (rsa_decrypt(rsa, app->buf->data, app->buf->offset, dec) > 0)
		ret = mono_string_new(app->domain, dec->data);
	else
		ret = mono_string_new(app->domain, "");
	//buf_force_reset(app->buf);
	buf_force_reset(dec);
	buf_return(dec);
	rsa_free(rsa);
	return ret;
}

static MonoArray* mem_sign(int type, MonoString *key, MonoString *keypwd, MonoString *content)
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;

	switch (type) {
	case 0:
		type = NID_md5;
		break;
	case 1:
		type = NID_sha1;
		break;
	case 2:
		type = NID_sha256;
		break;
	case 3:
		type = NID_sha512;
		break;
	default:
		return NULL;
	}

	char *k = mono_string_to_utf8(key);
	if (!k)
		return NULL;

	char *c = mono_string_to_utf8(content);
	if (!c) {
		mono_free(k);
		return mono_array_new(app->domain, mono_get_byte_class(), 0);
	}

	char *p = mono_string_to_utf8(keypwd);
	RSA *rsa = rsa_new_from_mem(k, p);
	if (p)
		mono_free(p);
	if (!rsa) {
		mono_free(k);
		mono_free(c);
		return mono_array_new(app->domain, mono_get_byte_class(), 0);
	}
	mono_free(k);

	char sign[RSA_size(rsa) + 1];
	memset(sign, 0, sizeof(sign));
	int len = rsa_sign(rsa, type, c, strlen(c), sign);
	MonoArray *ret = mono_array_new(app->domain, mono_get_byte_class(), len);
	mono_array_out(ret, sign, len);
	mono_free(c);
	rsa_free(rsa);
	return ret;
}

static MonoBoolean mem_verify(int type, MonoString *key, MonoString *keypwd, MonoString *content, MonoArray *sign)
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;

	switch (type) {
	case 0:
		type = NID_md5;
		break;
	case 1:
		type = NID_sha1;
		break;
	case 2:
		type = NID_sha256;
		break;
	case 3:
		type = NID_sha512;
		break;
	default:
		return 0;
	}

	char *k = mono_string_to_utf8(key);
	if (!k)
		return 0;

	char *c = mono_string_to_utf8(content);
	if (!c) {
		mono_free(k);
		return 0;
	}

	char *p = mono_string_to_utf8(keypwd);
	RSA *rsa = rsa_new_from_mem(k, p);
	if (p)
		mono_free(p);
	if (!rsa) {
		mono_free(k);
		mono_free(c);
		return 0;
	}
	mono_free(k);

	int len = mono_array_length(sign);
	if (len <= 0) {
		mono_free(c);
		rsa_free(rsa);
		return 0;
	}
	mono_array_in(sign, len, app->buf);
	int ret = rsa_verify(rsa, type, c, strlen(c), app->buf->data, app->buf->offset);
	//buf_force_reset(app->buf);
	mono_free(c);
	rsa_free(rsa);
	return ret;
}


static icall_item_t __items[] = {
	ICALL_ITEM(RsaEncrypt, __encrypt),
	ICALL_ITEM(RsaDecrypt, __decrypt),
	ICALL_ITEM(RsaSign, sign),
	ICALL_ITEM(RsaVerify, verify),
	ICALL_ITEM(RsaMemEncrypt, mem_encrypt),
	ICALL_ITEM(RsaMemDecrypt, mem_decrypt),
	ICALL_ITEM(RsaMemSign, mem_sign),
	ICALL_ITEM(RsaMemVerify, mem_verify),
	ICALL_ITEM_NULL
};

void reg_rsa() { regit(__items); }
