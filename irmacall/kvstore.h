#ifndef __KVSTORE_H__
#define __KVSTORE_H__

#ifdef __cplusplus
extern "C" {
#endif

#include <time.h>
#include "../config.h"

typedef struct __kvs kvs_t;
struct __kvs {
	void		*priv;
	void		(*free)(void *s);
	int			(*expire)(kvs_t *s, const char *key, time_t exp);
	int			(*exists)(kvs_t *s, const char *key);
	int			(*del)(kvs_t *s, const char *key);
	int			(*setnx)(kvs_t *s, const char *key, const char *val, int vlen, time_t exp);
	int			(*setex)(kvs_t *s, const char *key, const char *val, int vlen, time_t exp);
	int			(*set)(kvs_t *s, const char *key, const char *val, int vlen, time_t exp);
	int			(*add)(kvs_t *s, const char *key, const char *val, int vlen, time_t exp);
	int			(*replace)(kvs_t *s, const char *key, const char *val, int vlen, time_t exp);
	int			(*prepend)(kvs_t *s, const char *key, const char *val, int vlen);
	int			(*append)(kvs_t *s, const char *key, const char *val, int vlen);
	const char*	(*get)(kvs_t *s, const char *key, int *vlen);
	int			(*mget)(kvs_t *s, int kc, const char **keys, char **vals, int *vlens, int *vfree);
	int			(*setcounter)(kvs_t *s, const char *key, long val);
	long		(*getcounter)(kvs_t *s, const char *key);
	long		(*incr)(kvs_t *s, const char *key, long val);
	long		(*decr)(kvs_t *s, const char *key, long val);
	long		(*llen)(kvs_t *s, const char *key);
	long		(*rpush)(kvs_t *s, const char *key, int vc, const char **vals, const int *vlens);
	const char*	(*lpop)(kvs_t *s, const char *key, int *vlen);
};

#ifdef SUPPORT_MEMCACHED
kvs_t* memcached_new(const char *servers, const char *prefix, long zipmin);
#endif

#ifdef SUPPORT_REDIS
kvs_t* redis_new(const char *server, int port, const char *prefix);
#endif

#ifdef __cplusplus
}
#endif

#endif
