#include <string.h>
#include <assert.h>
#include <libmemcached/memcached.h>
#include "misc.h"
#include "wrapper.h"
#include "kvstore.h"
#include "service.h"

/*
 * libmemcached: https://libmemcached.org/
 */
#define MI			((memcached_inner_t*)s->priv)
#define KFMT		"c-%s-%s"
#define __(key)		key = buf_printf(MI->key, KFMT, MI->prefix, key)
#define K			MI->key->data
#define KLEN		MI->key->offset
#define ZIPMIN		1024
#define ZIPDEF		10240

typedef struct {
	memcached_st *m;
	memcached_return_t ret;
	char *prefix;
	buf_t *key, *val;
	long zipmin;
	qlz_state_compress qsc;
	qlz_state_decompress qsd;
} memcached_inner_t;

static void m_log(const char *func, const char *key, kvs_t *s)
{
	/* Refer to include/libmemcached-1.0/types/return.h */
	static const char *r[] = {
	"MEMCACHED_SUCCESS",
	"MEMCACHED_FAILURE",
	"MEMCACHED_HOST_LOOKUP_FAILURE",
	"MEMCACHED_CONNECTION_FAILURE",
	"MEMCACHED_CONNECTION_BIND_FAILURE",
	"MEMCACHED_WRITE_FAILURE",
	"MEMCACHED_READ_FAILURE",
	"MEMCACHED_UNKNOWN_READ_FAILURE",
	"MEMCACHED_PROTOCOL_ERROR",
	"MEMCACHED_CLIENT_ERROR",
	"MEMCACHED_SERVER_ERROR",
	"MEMCACHED_ERROR",
	"MEMCACHED_DATA_EXISTS",
	"MEMCACHED_DATA_DOES_NOT_EXIST",
	"MEMCACHED_NOTSTORED",
	"MEMCACHED_STORED",
	"MEMCACHED_NOTFOUND",
	"MEMCACHED_MEMORY_ALLOCATION_FAILURE",
	"MEMCACHED_PARTIAL_READ",
	"MEMCACHED_SOME_ERRORS",
	"MEMCACHED_NO_SERVERS",
	"MEMCACHED_END",
	"MEMCACHED_DELETED",
	"MEMCACHED_VALUE",
	"MEMCACHED_STAT",
	"MEMCACHED_ITEM",
	"MEMCACHED_ERRNO",
	"MEMCACHED_FAIL_UNIX_SOCKET",
	"MEMCACHED_NOT_SUPPORTED",
	"MEMCACHED_NO_KEY_PROVIDED",
	"MEMCACHED_FETCH_NOTFINISHED",
	"MEMCACHED_TIMEOUT",
	"MEMCACHED_BUFFERED",
	"MEMCACHED_BAD_KEY_PROVIDED",
	"MEMCACHED_INVALID_HOST_PROTOCOL",
	"MEMCACHED_SERVER_MARKED_DEAD",
	"MEMCACHED_UNKNOWN_STAT_KEY",
	"MEMCACHED_E2BIG",
	"MEMCACHED_INVALID_ARGUMENTS",
	"MEMCACHED_KEY_TOO_BIG",
	"MEMCACHED_AUTH_PROBLEM",
	"MEMCACHED_AUTH_FAILURE",
	"MEMCACHED_AUTH_CONTINUE",
	"MEMCACHED_PARSE_ERROR",
	"MEMCACHED_PARSE_USER_ERROR",
	"MEMCACHED_DEPRECATED",
	"MEMCACHED_IN_PROGRESS",
	"MEMCACHED_SERVER_TEMPORARILY_DISABLED",
	"MEMCACHED_SERVER_MEMORY_ALLOCATION_FAILURE",
	"MEMCACHED_MAXIMUM_RETURN",
	"MEMCACHED_CONNECTION_SOCKET_CREATE_FAILURE"
	};
	DEBUG(CURRENT->log, "%s - key=%s`ret=%s", func, key, r[MI->ret]);
}

static memcached_inner_t* new_memcached_inner(const char *servers, const char *prefix, long zipmin)
{
	if (!servers)
		servers = "--SERVER=127.0.0.1:11211";
	memcached_st *m = memcached(servers, strlen(servers));
	if (!m)
		return NULL;
	memcached_inner_t *mi = xcalloc(1, sizeof(*mi));
	mi->m = m;
	mi->prefix = xstrdup(prefix);
	mi->key = buf_new();
	mi->val = buf_new();
	mi->zipmin = (zipmin == 0L) ? ZIPDEF : (zipmin < ZIPMIN ? ZIPMIN : zipmin);
	return mi;
}

static const char* __zip(kvs_t *s, const char *src, int *len)
{
	buf_data(MI->val, *len);
	MI->val->offset = *len = zip(src, *len, MI->val->data, &MI->qsc);
	return MI->val->data;
}

static const char* __unzip(kvs_t *s, const char *src, int *len)
{
	buf_data(MI->val, unziplen(src));
	MI->val->offset = *len = unzip(src, MI->val->data, &MI->qsd);
	return MI->val->data;
}

static void m_free(void *memc)
{
	assert(memc);
	kvs_t *s = (kvs_t*)memc;
	if (MI->m)
		memcached_free(MI->m);
	if (MI->prefix)
		free(MI->prefix);
	if (MI->key)
		buf_free(MI->key);
	if (MI->val)
		buf_free(MI->val);
	free(MI);
	free(s);
}

static int m_expire(kvs_t *s, const char *key, time_t exp)
{
	assert(key);
	__(key);
	MI->ret = memcached_touch(MI->m, K, KLEN, exp);
	//m_log(__func__, K, s);
	return MI->ret == MEMCACHED_SUCCESS;
}

static int m_exists(kvs_t *s, const char *key)
{
	assert(key);
	__(key);
	MI->ret = memcached_exist(MI->m, K, KLEN);
	//m_log(__func__, K, s);
	return MI->ret == MEMCACHED_SUCCESS;
}

static int m_del(kvs_t *s, const char *key)
{
	assert(key);
	__(key);
	MI->ret = memcached_delete(MI->m, K, KLEN, 0);
	//m_log(__func__, K, s);
	return MI->ret == MEMCACHED_SUCCESS;
}

static int m_setnx(kvs_t *s, const char *key, const char *val, int vlen, time_t exp)
{
	return s->add(s, key, val, vlen, exp);
}

static int m_setex(kvs_t *s, const char *key, const char *val, int vlen, time_t exp)
{
	return s->replace(s, key, val, vlen, exp);
}

static int m_set(kvs_t *s, const char *key, const char *val, int vlen, time_t exp)
{
	assert(key && val && vlen > 0);
	__(key);
	uint32_t flags = (vlen > MI->zipmin);
	if (flags)
		val = __zip(s, val, &vlen);
	MI->ret = memcached_set(MI->m, K, KLEN, val, (size_t)vlen, exp, flags);
	//m_log(__func__, K, s);
	return MI->ret == MEMCACHED_SUCCESS;
}

static int m_add(kvs_t *s, const char *key, const char *val, int vlen, time_t exp)
{
	assert(key && val && vlen > 0);
	__(key);
	uint32_t flags = (vlen > MI->zipmin);
	if (flags)
		val = __zip(s, val, &vlen);
	MI->ret = memcached_add(MI->m, K, KLEN, val, (size_t)vlen, exp, flags);
	//m_log(__func__, K, s);
	return MI->ret == MEMCACHED_SUCCESS;
}

static int m_replace(kvs_t *s, const char *key, const char *val, int vlen, time_t exp)
{
	assert(key && val && vlen > 0);
	__(key);
	uint32_t flags = (vlen > MI->zipmin);
	if (flags)
		val = __zip(s, val, &vlen);
	MI->ret = memcached_replace(MI->m, K, KLEN, val, (size_t)vlen, exp, flags);
	//m_log(__func__, K, s);
	return MI->ret == MEMCACHED_SUCCESS;
}

static int m_append(kvs_t *s, const char *key, const char *val, int vlen)
{
	/*
	 * “flags” is a 4byte space that is stored alongside of the main value. Many sub
	 * libraries make use of this field, so in most cases users should avoid making use of it.
	 * -- Refer to http://docs.libmemcached.org/memcached_append.html#memcached_append
	 *
	 * So, in order to avoid disturbing data stored by appending, it seems as if you might do:
	 * s->set(s, key, "#", 1, exp); # make sure flags is 0. and then
	 * s->append(s, key, val, vlen);
	 * ...
	 * Skip the first byte after retrieving data to use.
	 */
	assert(key && val && vlen > 0);
	__(key);
	time_t exp = 0;
	uint32_t flags = 0;
	MI->ret = memcached_append(MI->m, K, KLEN, val, (size_t)vlen, exp, flags);
	//m_log(__func__, K, s);
	return MI->ret == MEMCACHED_SUCCESS;
}

static int m_prepend(kvs_t *s, const char *key, const char *val, int vlen)
{
	assert(key && val && vlen > 0);
	__(key);
	time_t exp = 0;
	uint32_t flags = 0;
	MI->ret = memcached_prepend(MI->m, K, KLEN, val, (size_t)vlen, exp, flags);
	//m_log(__func__, K, s);
	return MI->ret == MEMCACHED_SUCCESS;
}

static const char* m_get(kvs_t *s, const char *key, int *vlen)
{
	assert(key);
	__(key);
	uint32_t flags;
	const char *val = memcached_get(MI->m, K, KLEN, (size_t*)vlen, &flags, &MI->ret);
	if (flags)
		val = __unzip(s, val, vlen);
	//m_log(__func__, K, s);
	return MI->ret == MEMCACHED_SUCCESS ? val : NULL;
}

static int m_mget(kvs_t *s, int kc, const char **keys, char **vals, int *vlens, int *vfree)
{
	assert(kc > 0 && keys && vals && vlens);
	char **pks = xcalloc(kc, sizeof(char*));
	/* FIX: must be size_t here ! */
	size_t *pklens = xcalloc(kc, sizeof(size_t));
	int i = 0;
	for (; i < kc; i++) {
		buf_printf(MI->key, KFMT, MI->prefix, keys[i]);
		pks[i] = xstrdup(K);
		pklens[i] = KLEN;
	}
	MI->ret = memcached_mget(MI->m, (const char* const*)pks, pklens, kc);
	//m_log(__func__, K, s);
	for (i = 0; i < kc; i++)
		free(pks[i]);
	free(pks);
	free(pklens);
	if (MI->ret != MEMCACHED_SUCCESS)
		return 0;
	/*
	 * memcached_result_create() will either allocate memory for a memcached_result_st
	 * or initialize a structure passed to it.
	 */
	memcached_result_st res_obj, *res = memcached_result_create(MI->m, &res_obj);
	i = 0;
	while ((res = memcached_fetch_result(MI->m, &res_obj, &MI->ret)) != NULL) {
		/*
		 * Honestly, I'm not sure if the returning order is really as same as that we
		 * are getting. If not you'd better set the correct value by mapping the key.
		 * NOTE: memcached_result_key_value(res) is the key.
		 */
		vlens[i] = memcached_result_length(res);
		if (vlens[i] > 0) {
			const char *src = memcached_result_value(res);
			if (memcached_result_flags(res) > 0) {
				vlens[i] = unziplen(src);
				vals[i] = xmalloc(vlens[i]);
				unzip(src, vals[i], &MI->qsd);
			} else {
				vals[i] = xmalloc(vlens[i]);
				memcpy(vals[i], src, vlens[i]);
			}
		}
		i++;
	}
	memcached_result_free(&res_obj);
	if (i > 0)
		*vfree = 1;
	return 1;
}

static int m_setcounter(kvs_t *s, const char *key, long val)
{
	if (s->set(s, key, "0", 1, 0L))
		return s->incr(s, key, val) == val;
	return 0;
}

static long m_getcounter(kvs_t *s, const char *key)
{
	return s->incr(s, key, 0L);
}

static long m_incr(kvs_t *s, const char *key, long val)
{
	assert(key);
	__(key);
	uint64_t final;
	MI->ret = memcached_increment(MI->m, K, KLEN, (uint32_t)val, &final);
	//m_log(__func__, K, s);
	return MI->ret == MEMCACHED_SUCCESS ? (long)final : -1L;
}

static long m_decr(kvs_t *s, const char *key, long val)
{
	assert(key);
	__(key);
	uint64_t final;
	MI->ret = memcached_decrement(MI->m, K, KLEN, (uint32_t)val, &final);
	//m_log(__func__, K, s);
	return MI->ret == MEMCACHED_SUCCESS ? (long)final : -1L;
}

kvs_t* memcached_new(const char *servers, const char *prefix, long zipmin)
{
	memcached_inner_t *mi = new_memcached_inner(servers, prefix, zipmin);
	if (!mi)
		return NULL;
	kvs_t *s = xcalloc(1, sizeof(*s));
	s->priv = mi;
	s->free = &m_free;
	s->expire = &m_expire;
	s->exists = &m_exists;
	s->del = &m_del;
	s->setnx = &m_setnx;
	s->setex = &m_setex;
	s->set = &m_set;
	s->add = &m_add;
	s->replace = &m_replace;
	s->prepend = &m_prepend;
	s->append = &m_append;
	s->get = &m_get;
	s->mget = &m_mget;
	s->setcounter = &m_setcounter;
	s->getcounter = &m_getcounter;
	s->incr = &m_incr;
	s->decr = &m_decr;

	// Test it.
	if (s->set(s, "test", "ok", 2, 0) != 1 || !s->get(s, "test", NULL)) {
		s->free(s);
		s = NULL;
	}
	return s;
}
