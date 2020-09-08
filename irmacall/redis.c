#include <string.h>
#include <assert.h>
#include <hiredis.h>
#include "wrapper.h"
#include "kvstore.h"
#include "service.h"

/*
 * redis: http://doc.redisfans.com/
 */
#define CXT		reset(s)
#define KFMT	"c-%s-%s"
#define __(key)	key = buf_printf(RI->key, KFMT, RI->prefix, key)
#define K		RI->key->data
#define KLEN	RI->key->offset
#define EXP		(exp == 0 ? 8640000 : exp)
#define LTMAX	2

#define RI ((redis_inner_t*)s->priv)

typedef struct {
	redisContext *context;
	redisReply *reply;
	char *prefix;
	buf_t *key;
	long lost;
} redis_inner_t;

static void r_log(const char *func, const char *key, kvs_t *s)
{
	/*
	Refer to file hiredis/read.h:

	#define REDIS_ERR_IO 1
	#define REDIS_ERR_EOF 3
	#define REDIS_ERR_PROTOCOL 4
	#define REDIS_ERR_OOM 5
	#define REDIS_ERR_OTHER 2

	#define REDIS_REPLY_STRING 1
	#define REDIS_REPLY_ARRAY 2
	#define REDIS_REPLY_INTEGER 3
	#define REDIS_REPLY_NIL 4
	#define REDIS_REPLY_STATUS 5
	#define REDIS_REPLY_ERROR 6
	*/
	static const char *e[] = { "",
	"REDIS_ERR_IO",
	"REDIS_ERR_OTHER",
	"REDIS_ERR_EOF",
	"REDIS_ERR_PROTOCOL",
	"REDIS_ERR_OOM"
	};
	static const char *t[] = { "",
	"REDIS_REPLY_STRING",
	"REDIS_REPLY_ARRAY",
	"REDIS_REPLY_INTEGER",
	"REDIS_REPLY_NIL",
	"REDIS_REPLY_STATUS",
	"REDIS_REPLY_ERROR"
	};
	if (RI->reply)
		DEBUG(CURRENT->log, "%s - key=%s`cxt.fd=%d`cxt.flags=%d`cxt.err=%s`cxt.errstr=%s`r.type=%s`r.integer=%lld`r.len=%d`r.str-is-null=%s`r.str=%s", \
		func, key, RI->context->fd, RI->context->flags, e[RI->context->err], RI->context->err > 0 ? RI->context->errstr : "", \
		t[RI->reply->type], RI->reply->integer, RI->reply->len, RI->reply->str ? "false" : "true", RI->reply->str);
	else
		DEBUG(CURRENT->log, "%s - key=%s`cxt.fd=%d`cxt.flags=%d`cxt.err=%s`cxt.errstr=%s`r=null", \
		func, key, RI->context->fd, RI->context->flags, e[RI->context->err], RI->context->err > 0 ? RI->context->errstr : "");
}

static redis_inner_t* new_redis_inner(const char *server, int port, const char *prefix)
{
	if (!server)
		server = "127.0.0.1";
	if (port <= 0)
		port = 6379;
	struct timeval timeout = { 2, 500000 }; //2.5 seconds
	redisContext *c = redisConnectWithTimeout(server, port, timeout);
	if (!c)
		return NULL;
	if (c->err || !(c->flags & REDIS_CONNECTED)) {
		redisFree(c);
		return NULL;
	}
	redis_inner_t *ri = xcalloc(1, sizeof(*ri));
	ri->context = c;
	ri->prefix = xstrdup(prefix);
	ri->key = buf_new();
	return ri;
}

static redisContext* reset(kvs_t *s)
{
	if (RI->reply) {
		freeReplyObject(RI->reply);
		RI->reply = NULL;
	}
	RI->context->err = 0;
	return RI->context;
}

static void reconn(kvs_t *s)
{
	/*
	 * SIGPIPE(13) signal will be caught in sig_handle_main() of service.c
	 * if try to write to a closed socket. let it be.
	 */
	if (RI->context->err == REDIS_ERR_EOF || RI->context->err == REDIS_ERR_IO) {
		if (++RI->lost < 0L)
			RI->lost = 1L;
		if (RI->lost > LTMAX && redisReconnect(RI->context) == REDIS_OK) {
			RI->lost = 0L;
			EVENT(CURRENT->log, "Core - Redis ('%s') reconnect successfully", RI->prefix);
			return;
		}
		if (RI->lost < 10L || !(RI->lost % 100))
			WARN(CURRENT->log, "Core - Redis ('%s') disconnection detected (times: %ld)", RI->prefix, RI->lost);
	}
}

static void r_free(void *redis)
{
	assert(redis);
	kvs_t *s = (kvs_t*)redis;
	if (RI->reply)
		freeReplyObject(RI->reply);
	if (RI->context)
		redisFree(RI->context);
	if (RI->prefix)
		free(RI->prefix);
	if (RI->key)
		buf_free(RI->key);
	free(RI);
	free(s);
}

static int r_expire(kvs_t *s, const char *key, time_t exp)
{
	assert(key);
	__(key);
	RI->reply = redisCommand(CXT, "EXPIRE %s %lu", K, EXP);
	//r_log(__func__, K, s);
	if (!RI->reply) {
		reconn(s);
		return -1;
	}
	if (RI->reply->type == REDIS_REPLY_ERROR)
		return -1;
	/*
	 * reply->type == REDIS_REPLY_INTEGER
	 * reply->integer > 0: success
	 */
	return RI->reply->integer > 0;
}

static int r_exists(kvs_t *s, const char *key)
{
	assert(key);
	__(key);
	RI->reply = redisCommand(CXT, "EXISTS %s", K);
	//r_log(__func__, K, s);
	if (!RI->reply) {
		reconn(s);
		return -1;
	}
	if (RI->reply->type == REDIS_REPLY_ERROR)
		return -1;
	/*
	 * reply->type == REDIS_REPLY_INTEGER
	 * reply->integer > 0: success
	 */
	return RI->reply->integer > 0;
}

static int r_del(kvs_t *s, const char *key)
{
	assert(key);
	__(key);
	RI->reply = redisCommand(CXT, "DEL %s", K);
	//r_log(__func__, K, s);
	if (!RI->reply) {
		reconn(s);
		return -1;
	}
	if (RI->reply->type == REDIS_REPLY_ERROR)
		return -1;
	/*
	 * reply->type == REDIS_REPLY_INTEGER
	 * reply->integer > 0: success
	 */
	return RI->reply->integer > 0;
}

static int r_setnx(kvs_t *s, const char *key, const char *val, int vlen, time_t exp)
{
	assert(key && val && vlen > 0);
	if (exp >= 0)
		return s->exists(s, key) ? 0 : s->set(s, key, val, vlen, exp);
	__(key);
	const char *a[] = { "SETNX", K, val };
	const size_t alen[] = { 5, KLEN, vlen };
	RI->reply = redisCommandArgv(CXT, 3, a, alen);
	//r_log(__func__, K, s);
	if (!RI->reply) {
		reconn(s);
		return -1;
	}
	if (RI->reply->type == REDIS_REPLY_ERROR)
		return -1;
	/*
	 * reply->type == REDIS_REPLY_INTEGER
	 * reply->integer > 0: success
	 */
	return RI->reply->integer > 0;
}

static int r_setex(kvs_t *s, const char *key, const char *val, int vlen, time_t exp)
{
	assert(key && val && vlen > 0);
	if (exp >= 0)
		return s->exists(s, key) ? s->set(s, key, val, vlen, exp) : 0;
	__(key);
	const char *a[] = { "SETEX", K, val };
	const size_t alen[] = { 5, KLEN, vlen };
	RI->reply = redisCommandArgv(CXT, 3, a, alen);
	//r_log(__func__, K, s);
	if (!RI->reply) {
		reconn(s);
		return -1;
	}
	if (RI->reply->type == REDIS_REPLY_ERROR)
		return -1;
	/*
	 * reply->type == REDIS_REPLY_INTEGER
	 * reply->integer > 0: success
	 */
	return RI->reply->integer > 0;
}

static int r_set(kvs_t *s, const char *key, const char *val, int vlen, time_t exp)
{
	assert(key && val && vlen > 0);
	__(key);
	char buf[16];
	snprintf(buf, 16, "%lu", EXP);
	const char *a[] = { "SET", K, val, "EX", buf };
	const size_t alen[] = { 3, KLEN, vlen, 2, strlen(buf) };
	RI->reply = redisCommandArgv(CXT, 5, a, alen);
	//r_log(__func__, K, s);
	if (!RI->reply) {
		reconn(s);
		return -1;
	}
	if (RI->reply->type == REDIS_REPLY_ERROR)
		return -1;
	/*
	 * reply->type == REDIS_REPLY_STATUS
	 * reply->integer == 0: success
	 * reply->str == 'OK': success
	 */
	return RI->reply->integer == 0;
}

static int r_add(kvs_t *s, const char *key, const char *val, int vlen, time_t exp)
{
	return s->setnx(s, key, val, vlen, exp);
}

static int r_replace(kvs_t *s, const char *key, const char *val, int vlen, time_t exp)
{
	return s->setex(s, key, val, vlen, exp);
}

static int r_append(kvs_t *s, const char *key, const char *val, int vlen)
{
	assert(key && val && vlen > 0);
	__(key);
	const char *a[] = { "APPEND", K, val };
	const size_t alen[] = { 6, KLEN, vlen };
	RI->reply = redisCommandArgv(CXT, 3, a, alen);
	//r_log(__func__, K, s);
	if (!RI->reply) {
		reconn(s);
		return -1;
	}
	if (RI->reply->type == REDIS_REPLY_ERROR)
		return -1;
	/*
	 * reply->type == REDIS_REPLY_INTEGER
	 * reply->integer > 0: success
	 */
	return RI->reply->integer > 0;
}

static const char* r_get(kvs_t *s, const char *key, int *vlen)
{
	assert(key);
	__(key);
	RI->reply = redisCommand(CXT, "GET %s", K);
	//r_log(__func__, K, s);
	if (!RI->reply) {
		reconn(s);
		return NULL;
	}
	if (RI->reply->type == REDIS_REPLY_ERROR)
		return NULL;
	/*
	 * reply->type == REDIS_REPLY_STRING
	 * reply->str != NULL: success
	 */
	if (vlen)
		*vlen = RI->reply->len;
	return RI->reply->str;
}

static int r_mget(kvs_t *s, int kc, const char **keys, char **vals, int *vlens, int *vfree)
{
	assert(kc > 0 && keys && vals && vlens);
	/*
	 * redisCommand parses and separates fmt by space (It's different from sprintf, which uses '\0').
	 * so it will only print the first key if there are space in k:
	 * RI->reply = redisCommand(RI->context, "MGET %s", K);
	 */
	int i, ret = 0;
	buf_printf(RI->key, "MGET "KFMT, RI->prefix, keys[0]);
	for (i = 1; i < kc; i++)
		buf_printf_ext(RI->key, " "KFMT, RI->prefix, keys[i]);
	RI->reply = redisCommand(CXT, K);
	//r_log(__func__, K+5, s);
	if (!RI->reply) {
		reconn(s);
		goto __exit;
	}
	if (RI->reply->type != REDIS_REPLY_ARRAY)
		goto __exit;
	/*
	 * reply->type == REDIS_REPLY_ARRAY
	 * reply->elements == kc
	 * reply->str != NULL: success
	 */
	for (i = 0; i < RI->reply->elements; i++) {
		vals[i] = RI->reply->element[i]->str;
		vlens[i] = RI->reply->element[i]->len;
	}
	*vfree = 0;
	ret = 1;
__exit:
	buf_reset(RI->key);
	return ret;
}

static int r_setcounter(kvs_t *s, const char *key, long val)
{
	if (s->set(s, key, "0", 1, 0L))
		return s->incr(s, key, val) == val;
	return 0;
}

static long r_getcounter(kvs_t *s, const char *key)
{
	return s->incr(s, key, 0L);
}

static long r_incr(kvs_t *s, const char *key, long val)
{
	assert(key);
	__(key);
	RI->reply = redisCommand(CXT, "INCRBY %s %ld", K, val);
	//r_log(__func__, K, s);
	if (!RI->reply) {
		reconn(s);
		return -1L;
	}
	if (RI->reply->type == REDIS_REPLY_ERROR)
		return -1L;
	/*
	 * reply->type == REDIS_REPLY_INTEGER
	 * success always whatever 0 or negative etc.
	 */
	return (long)RI->reply->integer;
}

static long r_decr(kvs_t *s, const char *key, long val)
{
	assert(key);
	__(key);
	RI->reply = redisCommand(CXT, "DECRBY %s %ld", K, val);
	//r_log(__func__, K, s);
	if (!RI->reply) {
		reconn(s);
		return -1L;
	}
	if (RI->reply->type == REDIS_REPLY_ERROR)
		return -1L;
	/*
	 * reply->type == REDIS_REPLY_INTEGER
	 * success always whatever 0 or negative etc.
	 */
	return (long)RI->reply->integer;
}

static long r_llen(kvs_t *s, const char *key)
{
	assert(key);
	__(key);
	RI->reply = redisCommand(CXT, "LLEN %s", K);
	//r_log(__func__, K, s);
	if (!RI->reply) {
		reconn(s);
		return -1L;
	}
	if (RI->reply->type == REDIS_REPLY_ERROR)
		return -1L;
	/*
	 * reply->type == REDIS_REPLY_INTEGER
	 * reply->integer > 0: success
	 */
	return (long)RI->reply->integer;
}

static long r_rpush(kvs_t *s, const char *key, int vc, const char **vals, const int *vlens)
{
	assert(key && vc > 0 && vals && vlens);
	__(key);
	int i;
	long ret = -1L;
	char **a = (char**)xcalloc(2 + vc, sizeof(char*));
	size_t *alen = (size_t*)xcalloc(2 + vc, sizeof(size_t));
	a[0] = "RPUSH";
	a[1] = K;
	alen[0] = (size_t)5;
	alen[1] = (size_t)KLEN;
	for (i = 0; i < vc; i++) {
		a[2+i] = (char*)vals[i];
		alen[2+i] = (size_t)vlens[i];
	}
	RI->reply = redisCommandArgv(CXT, 2 + vc, (const char**)a, (const size_t*)alen);
	//r_log(__func__, K, s);
	if (!RI->reply) {
		reconn(s);
		goto __exit;
	}
	if (RI->reply->type == REDIS_REPLY_ERROR)
		goto __exit;
	/*
	 * reply->type == REDIS_REPLY_INTEGER
	 * reply->integer > 0: success
	 */
	ret = (long)RI->reply->integer;
__exit:
	free(a);
	free(alen);
	return ret;
}

static const char* r_lpop(kvs_t *s, const char *key, int *vlen)
{
	assert(key && vlen);
	__(key);
	RI->reply = redisCommand(CXT, "LPOP %s", K);
	//r_log(__func__, K, s);
	if (!RI->reply) {
		reconn(s);
		return NULL;
	}
	if (RI->reply->type == REDIS_REPLY_ERROR)
		return NULL;
	/*
	 * reply->type == REDIS_REPLY_STRING
	 * reply->str != NULL: success
	 */
	if (vlen)
		*vlen = RI->reply->len;
	return RI->reply->str;
}

kvs_t* redis_new(const char *server, int port, const char *prefix)
{
	redis_inner_t *ri = new_redis_inner(server, port, prefix);
	if (!ri)
		return NULL;
	kvs_t *s = xcalloc(1, sizeof(*s));
	s->priv = ri;
	s->free = &r_free;
	s->expire = &r_expire;
	s->exists = &r_exists;
	s->del = &r_del;
	s->setnx = &r_setnx;
	s->setex = &r_setex;
	s->set = &r_set;
	s->add = &r_add;
	s->replace = &r_replace;
	s->append = &r_append;
	s->get = &r_get;
	s->mget = &r_mget;
	s->setcounter = &r_setcounter;
	s->getcounter = &r_getcounter;
	s->incr = &r_incr;
	s->decr = &r_decr;
	s->llen = &r_llen;
	s->rpush = &r_rpush;
	s->lpop = &r_lpop;

	// Test it.
	if (s->set(s, "test", "ok", 2, 0) != 1 || !s->get(s, "test", NULL)) {
		s->free(s);
		s = NULL;
	}
	return s;
}
