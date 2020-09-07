#include "icall.h"

#define __L	(L > 0 ? (kvs_t*)L : NULL)
#define EXP	(exp < 0 ? 0 : (time_t)exp)

#ifdef SUPPORT_MEMCACHED
static long new_memcached(MonoString *servers, MonoString *instance, long zipmin)
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;
	kvs_t *s = NULL;

	char *ps = mono_string_to_utf8(servers);
	char *pi = mono_string_to_utf8(instance);
	if (!pi)
		goto __exit;
	buf_printf(app->buf, "kvs-memcached-%s", pi);
	if (!(s = (kvs_t*)app->map->get(app->map, app->buf->data))) {
		if ((s = memcached_new(ps, pi, zipmin)) != NULL)
			app->map->set(app->map, app->buf->data, s, s->free);
	}
	mono_free(pi);
__exit:
	if (ps)
		mono_free(ps);
	return (long)s;
}
#endif

#ifdef SUPPORT_REDIS
static long new_redis(MonoString *server, int port, MonoString *instance)
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;
	kvs_t *s = NULL;

	char *ps = mono_string_to_utf8(server);
	char *pi = mono_string_to_utf8(instance);
	if (!pi)
		goto __exit;
	buf_printf(app->buf, "kvs-redis-%s", pi);
	if (!(s = (kvs_t*)app->map->get(app->map, app->buf->data))) {
		if ((s = redis_new(ps, port, pi)) != NULL)
			app->map->set(app->map, app->buf->data, s, s->free);
	}
	mono_free(pi);
__exit:
	if (ps)
		mono_free(ps);
	return (long)s;
}
#endif

static MonoBoolean expire(long L, MonoString *key, long exp)
{
	kvs_t *s = __L;
	if (!s || !s->expire)
		return 0;
	char *k = mono_string_to_utf8(key);
	if (!k)
		return 0;
	int ret = s->expire(s, k, EXP);
	mono_free(k);
	return ret < 0 ? 0 : ret;
}

static MonoBoolean exists(long L, MonoString *key)
{
	kvs_t *s = __L;
	if (!s)
		return 0;
	char *k = mono_string_to_utf8(key);
	if (!k)
		return 0;
	int ret = s->exists(s, k);
	mono_free(k);
	return ret < 0 ? 0 : ret;
}

static MonoBoolean delete(long L, MonoString *key)
{
	kvs_t *s = __L;
	if (!s)
		return 0;
	char *k = mono_string_to_utf8(key);
	if (!k)
		return 0;
	int ret = s->del(s, k);
	mono_free(k);
	return ret;
}

static MonoBoolean setnx(long L, MonoString *key, MonoArray *val, long exp)
{
	kvs_t *s = __L;
	if (!s || !s->setnx)
		return 0;
	char *k = mono_string_to_utf8(key);
	if (!k)
		return 0;
	int len = mono_array_length(val);
	if (len <= 0) {
		mono_free(k);
		return 0;
	}
	app_t *app = (app_t*)CURRENT->priv_app;
	buf_data_reset(app->buf, len);
	int i = 0;
	unsigned char uc;
	for (; i < len; i++) {
		uc = mono_array_get(val, unsigned char, i);
		buf_append(app->buf, (const char*)&uc, 1);
	}
	int ret = s->setnx(s, k, app->buf->data, app->buf->offset, EXP);
	mono_free(k);
	return ret;
}

static MonoBoolean setex(long L, MonoString *key, MonoArray *val, long exp)
{
	kvs_t *s = __L;
	if (!s || !s->setex)
		return 0;
	char *k = mono_string_to_utf8(key);
	if (!k)
		return 0;
	int len = mono_array_length(val);
	if (len <= 0) {
		mono_free(k);
		return 0;
	}
	app_t *app = (app_t*)CURRENT->priv_app;
	buf_data_reset(app->buf, len);
	int i = 0;
	unsigned char uc;
	for (; i < len; i++) {
		uc = mono_array_get(val, unsigned char, i);
		buf_append(app->buf, (const char*)&uc, 1);
	}
	int ret = s->setex(s, k, app->buf->data, app->buf->offset, EXP);
	mono_free(k);
	return ret;
}

static MonoBoolean set(long L, MonoString *key, MonoArray *val, long exp)
{
	kvs_t *s = __L;
	if (!s)
		return 0;
	char *k = mono_string_to_utf8(key);
	if (!k)
		return 0;
	int len = mono_array_length(val);
	if (len <= 0) {
		mono_free(k);
		return 0;
	}
	app_t *app = (app_t*)CURRENT->priv_app;
	buf_data_reset(app->buf, len);
	int i = 0;
	unsigned char uc;
	for (; i < len; i++) {
		uc = mono_array_get(val, unsigned char, i);
		buf_append(app->buf, (const char*)&uc, 1);
	}
	int ret = s->set(s, k, app->buf->data, app->buf->offset, EXP);
	mono_free(k);
	return ret;
}

static MonoArray* get(long L, MonoString *key)
{
	kvs_t *s = __L;
	if (!s)
		return NULL;
	char *k = mono_string_to_utf8(key);
	if (!k)
		return NULL;
	app_t *app = (app_t*)CURRENT->priv_app;
	int vlen = 0;
	const char *v = s->get(s, k, &vlen);
	mono_free(k);
	if (!v || vlen <= 0) {
		/* return mono_array_new(app->domain, mono_get_byte_class(), 0); */
		return NULL;
	}
	MonoArray *val = mono_array_new(app->domain, mono_get_byte_class(), vlen);
	int i = 0;
	for (; i < vlen; i++)
		mono_array_set(val, unsigned char, i, ((unsigned char*)v)[i]);
	return val;
}

static MonoArray* mget(long L, MonoArray *keys)
{
	kvs_t *s = __L;
	if (!s || !s->mget)
		return NULL;
	int kc = mono_array_length(keys);
	if (kc <= 0)
		return NULL;

	app_t *app = (app_t*)CURRENT->priv_app;
	char **ak = xcalloc(kc, sizeof(char*));
	char **av = xcalloc(kc, sizeof(char*));
	int *an = xcalloc(kc, sizeof(int));
	MonoArray *vals = NULL;
	int j, i = 0, vfree = 0;
	for (; i < kc; i++) {
		ak[i] = mono_string_to_utf8(mono_array_get(keys, MonoString*, i));
		if (!ak[i])
			goto __exit;
	}

	if (s->mget(s, kc, (const char**)ak, av, an, &vfree) == 0) {
		/* return mono_array_new(app->domain, mono_get_byte_class(), 0); */
		goto __exit;
	}

	vals = mono_array_new(app->domain, mono_get_array_class(), kc);
	for (i = 0; i < kc; i++) {
		MonoArray *val = mono_array_new(app->domain, mono_get_byte_class(), an[i]);
		for (j = 0; j < an[i]; j++)
			mono_array_set(val, unsigned char, j, ((unsigned char*)av[i])[j]);
		mono_array_set(vals, MonoArray*, i, val);
	}
__exit:
	for (i = 0; i < kc; i++) {
		if (ak[i])
			mono_free(ak[i]);
		if (vfree && av[i])
			free(av[i]);
	}
	free(ak);
	free(av);
	free(an);
	return vals;
}

static MonoBoolean add(long L, MonoString *key, MonoArray *val, long exp)
{
	kvs_t *s = __L;
	if (!s || !s->add)
		return 0;
	char *k = mono_string_to_utf8(key);
	if (!k)
		return 0;
	int len = mono_array_length(val);
	if (len <= 0) {
		mono_free(k);
		return 0;
	}
	app_t *app = (app_t*)CURRENT->priv_app;
	buf_data_reset(app->buf, len);
	int i = 0;
	unsigned char uc;
	for (; i < len; i++) {
		uc = mono_array_get(val, unsigned char, i);
		buf_append(app->buf, (const char*)&uc, 1);
	}
	int ret = s->add(s, k, app->buf->data, app->buf->offset, EXP);
	mono_free(k);
	return ret;
}

static MonoBoolean replace(long L, MonoString *key, MonoArray *val, long exp)
{
	kvs_t *s = __L;
	if (!s || !s->replace)
		return 0;
	char *k = mono_string_to_utf8(key);
	if (!k)
		return 0;
	int len = mono_array_length(val);
	if (len <= 0) {
		mono_free(k);
		return 0;
	}
	app_t *app = (app_t*)CURRENT->priv_app;
	buf_data_reset(app->buf, len);
	int i = 0;
	unsigned char uc;
	for (; i < len; i++) {
		uc = mono_array_get(val, unsigned char, i);
		buf_append(app->buf, (const char*)&uc, 1);
	}
	int ret = s->replace(s, k, app->buf->data, app->buf->offset, EXP);
	mono_free(k);
	return ret;
}

static MonoBoolean prepend(long L, MonoString *key, MonoArray *val)
{
	kvs_t *s = __L;
	if (!s || !s->prepend)
		return 0;
	char *k = mono_string_to_utf8(key);
	if (!k)
		return 0;
	int len = mono_array_length(val);
	if (len <= 0) {
		mono_free(k);
		return 0;
	}
	app_t *app = (app_t*)CURRENT->priv_app;
	buf_data_reset(app->buf, len);
	int i = 0;
	unsigned char uc;
	for (; i < len; i++) {
		uc = mono_array_get(val, unsigned char, i);
		buf_append(app->buf, (const char*)&uc, 1);
	}
	int ret = s->prepend(s, k, app->buf->data, app->buf->offset);
	mono_free(k);
	return ret;
}

static MonoBoolean append(long L, MonoString *key, MonoArray *val)
{
	kvs_t *s = __L;
	if (!s || !s->append)
		return 0;
	char *k = mono_string_to_utf8(key);
	if (!k)
		return 0;
	int len = mono_array_length(val);
	if (len <= 0) {
		mono_free(k);
		return 0;
	}
	app_t *app = (app_t*)CURRENT->priv_app;
	buf_data_reset(app->buf, len);
	int i = 0;
	unsigned char uc;
	for (; i < len; i++) {
		uc = mono_array_get(val, unsigned char, i);
		buf_append(app->buf, (const char*)&uc, 1);
	}
	int ret = s->append(s, k, app->buf->data, app->buf->offset);
	mono_free(k);
	return ret;
}

static MonoBoolean setcounter(long L, MonoString *key, long val)
{
	kvs_t *s = __L;
	if (!s || !s->setcounter)
		return 0;
	char *k = mono_string_to_utf8(key);
	if (!k)
		return 0;
	int ret = s->setcounter(s, k, val);
	mono_free(k);
	return ret;
}

static long getcounter(long L, MonoString *key)
{
	kvs_t *s = __L;
	if (!s || !s->getcounter)
		return -1L;
	char *k = mono_string_to_utf8(key);
	if (!k)
		return -1L;
	long ret = s->getcounter(s, k);
	mono_free(k);
	return ret;
}

static long incr(long L, MonoString *key, long val)
{
	kvs_t *s = __L;
	if (!s || !s->incr || val < 0)
		return -1L;
	char *k = mono_string_to_utf8(key);
	if (!k)
		return -1L;
	long ret = s->incr(s, k, val);
	mono_free(k);
	return ret;
}

static long decr(long L, MonoString *key, long val)
{
	kvs_t *s = __L;
	if (!s || !s->decr || val < 0)
		return -1L;
	char *k = mono_string_to_utf8(key);
	if (!k)
		return -1L;
	long ret = s->decr(s, k, val);
	mono_free(k);
	return ret;
}

static long llen(long L, MonoString *key)
{
	kvs_t *s = __L;
	if (!s || !s->llen)
		return -1L;
	char *k = mono_string_to_utf8(key);
	if (!k)
		return -1L;
	long ret = s->llen(s, k);
	mono_free(k);
	return ret;
}

static long rpush(long L, MonoString *key, MonoArray *vals)
{
	kvs_t *s = __L;
	if (!s || !s->rpush)
		return -1L;
	char *k = mono_string_to_utf8(key);
	if (!k)
		return -1L;
	int vc = mono_array_length(vals);
	if (vc <= 0) {
		mono_free(k);
		return -1L;
	}
	long ret = -1L;
	char **av = xcalloc(vc, sizeof(char*));
	int *an = xcalloc(vc, sizeof(int));
	int i = 0, j;
	for (; i < vc; i++) {
		MonoArray *array = mono_array_get(vals, MonoArray*, i);
		if (!array)
			goto __exit;
		if ((an[i] = mono_array_length(array)) <= 0)
			goto __exit;
		av[i] = (char*)xmalloc(an[i]);
		for (j = 0; j < an[i]; j++)
			av[i][j] = mono_array_get(array, unsigned char, j);
	}
	ret = s->rpush(s, k, vc, (const char**)av, an);
__exit:
	mono_free(k);
	for (i = 0; i < vc; i++) {
		if (av[i])
			free(av[i]);
	}
	free(av);
	free(an);
	return ret;
}

static MonoArray* lpop(long L, MonoString *key)
{
	kvs_t *s = __L;
	if (!s || !s->lpop)
		return NULL;
	char *k = mono_string_to_utf8(key);
	if (!k)
		return NULL;
	app_t *app = (app_t*)CURRENT->priv_app;
	int vlen = 0;
	const char *v = s->lpop(s, k, &vlen);
	mono_free(k);
	if (!v || vlen <= 0) {
		/* return mono_array_new(app->domain, mono_get_byte_class(), 0); */
		return NULL;
	}
	MonoArray *val = mono_array_new(app->domain, mono_get_byte_class(), vlen);
	int i = 0;
	for (; i < vlen; i++)
		mono_array_set(val, unsigned char, i, ((unsigned char*)v)[i]);
	return val;
}

static icall_item_t __items[] = {
	#ifdef SUPPORT_MEMCACHED
	ICALL_ITEM(MemcachedNew, new_memcached),
	#endif
	#ifdef SUPPORT_REDIS
	ICALL_ITEM(RedisNew, new_redis),
	#endif
	ICALL_ITEM(KvsExpire, expire),
	ICALL_ITEM(KvsExists, exists),
	ICALL_ITEM(KvsDelete, delete),
	ICALL_ITEM(KvsSetNx, setnx),
	ICALL_ITEM(KvsSetEx, setex),
	ICALL_ITEM(KvsSet, set),
	ICALL_ITEM(KvsGet, get),
	ICALL_ITEM(KvsMGet, mget),
	ICALL_ITEM(KvsAdd, add),
	ICALL_ITEM(KvsReplace, replace),
	ICALL_ITEM(KvsPrepend, prepend),
	ICALL_ITEM(KvsAppend, append),
	ICALL_ITEM(KvsSetCounter, setcounter),
	ICALL_ITEM(KvsGetCounter, getcounter),
	ICALL_ITEM(KvsIncr, incr),
	ICALL_ITEM(KvsDecr, decr),
	ICALL_ITEM(KvsLLen, llen),
	ICALL_ITEM(KvsRPush, rpush),
	ICALL_ITEM(KvsLPop, lpop),
	ICALL_ITEM_NULL
};

void reg_kvs() { regit(__items); }
