#include <ctype.h>
#include <string.h>
#include <stdarg.h>
#include <stdlib.h>
#include <assert.h>
#include "wrapper.h"
#include "list.h"
#include "buf.h"

#define BUFMIN			4
#define BUFLINE_MAX		1024
#define DEL_MINSIZE		10240
#define DEL_TIMES		200
#define RENEW_TIMES		100
#define RENEW_UR		0.618
#define RENEW_SFTIMES	3
#define BPF_LENT		0x01
#define BPF_AUTO		0x02
#define BUF_COUNT_MIN	4

#define BI ((buf_inner_t*)buf->priv)
#define PI ((pool_inner_t*)pool->priv)

typedef struct {
	int flag;
	int ltimes; /* lending times */
	int utimes; /* used times */
	int ftimes; /* continuing failure times */
	buf_pool_t *pool;
	litem_t *item;
} buf_inner_t;

typedef struct {
	list_t *busylist;
	list_t *freelist;
} pool_inner_t;

/*
 * API for buf_t
 */
static int __buf_init(buf_t *buf, int size)
{
	buf->offset = 0;
	buf->size = size < BUFMIN ? BUFMIN : size;
	buf->data = xcalloc(buf->size, sizeof(char));
	if (!buf->priv)
		buf->priv = xcalloc(1, sizeof(buf_inner_t));
	BI->utimes = 1;
	BI->ftimes = 0;
	return 0;
}

static char* __buf_data(buf_t *buf, unsigned int len)
{
	if (len + 1 > buf->size) {
		buf->size = (len - buf->size < BUFMIN) ? buf->size + BUFMIN : len + 1;
		buf->data = xrealloc(buf->data, buf->size);
	}
	return buf->data;
}

static void __buf_append(buf_t *buf, const char *data, int len)
{
	if (len <= 0)
		return;
	if (buf->offset + len + 1 > buf->size) {
		buf->size = buf->offset + len + BUFMIN;
		buf->data = xrealloc(buf->data, buf->size);
	}
	memcpy(buf->data + buf->offset, data, len);
	buf->offset += len;
	buf->data[buf->offset] = 0;
}

static int __buf_reset(buf_t *buf, int force)
{
	if (!buf->offset)
		return 0;
	if (force || ++BI->utimes > RENEW_TIMES)
		goto __renew;
	/* Utilization Rate check. */
	float ur = (float)buf->offset / (float)buf->size;
	if (ur > RENEW_UR)
		BI->ftimes = 0;
	else if (++BI->ftimes > RENEW_SFTIMES) {
__renew:
		free(buf->data);
		__buf_init(buf, 0);
		return 1;
	}
	buf->offset = 0;
	//memset(buf->data, 0, buf->size);
	buf->data[0] = '\0';
	return 0;
}

static buf_t* __buf_new(buf_pool_t *pool, int size)
{
	buf_t *buf = xcalloc(1, sizeof(buf_t));
	__buf_init(buf, size);
	BI->pool = pool;
	return buf;
}

buf_t* buf_new()
{
	return __buf_new(NULL, 0);
}

int buf_is_lent(buf_t *buf)
{
	assert(buf && buf->priv);
	return (BI->flag & BPF_LENT) ? 1 : 0;
}

int buf_is_auto(buf_t *buf)
{
	assert(buf && buf->priv);
	return (BI->flag & BPF_AUTO) ? 1 : 0;
}

char* buf_fgets(buf_t *buf, FILE *f)
{
	assert(buf && f);
	int newline = 0;
	char tmp[BUFLINE_MAX];

	__buf_reset(buf, 0);
	while (fgets(tmp, sizeof(tmp), f)) {
		int n = strlen(tmp);
		if (tmp[n - 1] == '\n') {
			tmp[--n] = 0;
			if (n > 0 && tmp[n - 1] == '\r')
				tmp[--n] = 0;
			newline = 1;
		}
		if (n > 0)
			__buf_append(buf, tmp, n);
		if (newline)
			break;
	}
	return newline ? buf->data : NULL;
}

char* buf_data(buf_t *buf, unsigned int len)
{
	assert(buf);
	return __buf_data(buf, len);
}

char* buf_printf(buf_t *buf, const char *fmt, ...)
{
	assert(buf && buf->size >= buf->offset);
	va_list v;
	va_start(v, fmt);
	int n = vsnprintf(buf->data, buf->size, fmt, v);
	va_end(v);
	if (n + 1 > buf->size) {
		buf->size = n + BUFMIN;
		buf->data = xrealloc(buf->data, buf->size);
		va_start(v, fmt);
		n = vsnprintf(buf->data, buf->size, fmt, v);
		va_end(v);
	}
	/*
	 * Note the type of buf->offset is unsigned int.
	 * Don't assign n to offset if n is negative.
	 */
	if (n < 0)
		__buf_reset(buf, 1);
	else
		buf->offset = n;
	return buf->data;
}

char* buf_printf_ext(buf_t *buf, const char *fmt, ...)
{
	assert(buf && buf->size >= buf->offset);
	char *p = buf->data + buf->offset;
	unsigned int size = buf->size - buf->offset;

	va_list v;
	va_start(v, fmt);
	int n = vsnprintf(p, size, fmt, v);
	va_end(v);
	if (n + 1 > size) {
		size = n + (BUFMIN << 1);
		buf->size = buf->offset + size;
		buf->data = xrealloc(buf->data, buf->size);
		p = buf->data + buf->offset;

		va_start(v, fmt);
		n = vsnprintf(p, size, fmt, v);
		va_end(v);
	}
	/*
	 * Note the type of buf->offset is unsigned int.
	 * Don't assign n to offset if n is negative.
	 */
	if (n < 0)
		__buf_reset(buf, 1);
	else
		buf->offset += n;
	return buf->data;
}

char* buf_trim(buf_t *buf)
{
	assert(buf && buf->size >= buf->offset);
	if (buf->offset <= 0)
		goto __exit;
	char *p = buf->data;
	while (*p && (isblank(*p) || isspace(*p) || iscntrl(*p)))
		p++;
	if (!*p) {
		buf->data[0] = '\0';
		buf->offset = 0;
		goto __exit;
	}
	char *q = buf->data + buf->offset - 1;
	while (isblank(*q) || isspace(*q) || iscntrl(*q))
		q--;
	*(q + 1) = 0;
	buf->offset = q + 1 - p;
	if (p != buf->data)
		buf->data = memmove(buf->data, p, buf->offset + 1);
__exit:
	return buf->data;
}

void buf_append(buf_t *buf, const char *data, int len)
{
	assert(buf && buf->size >= buf->offset);
	if (data)
		__buf_append(buf, data, len);
}

void buf_insert(buf_t *buf, char *where, const char *data, int len)
{
	assert(buf && buf->size >= buf->offset);
	if (!where || !data || len <= 0)
		return;
	int offset = where - buf->data;
	if (offset < 0 || offset > buf->offset)
		return;
	if (buf->offset + len + 1 > buf->size) {
		buf->size = buf->offset + len + BUFMIN;
		buf->data = xrealloc(buf->data, buf->size);
		where = buf->data + offset;
	}
	memmove(where + len, where, buf->offset - offset);
	memcpy(where, data, len);
	buf->offset += len;
	buf->data[buf->offset] = 0;
}

void buf_copy(const buf_t *from, buf_t *to)
{
	assert(from && to);
	__buf_reset(to, 0);
	__buf_append(to, from->data, from->offset);
}

void buf_reset(buf_t *buf)
{
	assert(buf);
	__buf_reset(buf, 0);
}

void buf_force_reset(buf_t *buf)
{
	assert(buf);
	__buf_reset(buf, 1);
}

void buf_data_reset(buf_t *buf, unsigned int len)
{
	assert(buf);
	__buf_reset(buf, 0);
	__buf_data(buf, len);
}

void buf_return(buf_t *buf)
{
	assert(buf);
	buf_pool_t *pool = BI->pool;
	if (!pool)
		buf_free(buf);
	else if (BI->item && (BI->flag & BPF_LENT)) {
		if (buf->size > DEL_MINSIZE && BI->ltimes > DEL_TIMES)
			PI->busylist->del(PI->busylist, BI->item);
		else {
			int prepend = 1;
			BI->flag &= ~BPF_LENT;
			if (!(BI->flag & BPF_AUTO))
				prepend = ~__buf_reset(buf, 0);
			PI->busylist->mv(BI->item, PI->busylist, PI->freelist, prepend);
		}
	}
}

void buf_free(void *data)
{
	if (data) {
		buf_t *buf = (buf_t*)data;
		if (buf->data)
			free(buf->data);
		if (buf->priv)
			free(buf->priv);
		free(buf);
	}
}

/*
 * API for buf_pool_t
 */
static buf_t* buf_pool_lend(buf_pool_t *pool, unsigned int size, int auto_reset)
{
	/*
	 * Different ways about how to ensure that the allocated memory of object borrowed from pool will be released safely:
	 * +-------------------------+---------------------------------------+-----------------------------------------------+
	 * |                         | AUTO                                  | ~AUTO                                         |
	 * +-------------------------+---------------------------------------+-----------------------------------------------+
	 * | will call buf_return()  | safe by pool->reset() or buf_return() | safe by buf_return                            |
	 * +-------------------------+---------------------------------------+-----------------------------------------------+
	 * | won't call buf_return() | safe by pool->reset()                 | unsafe & have to call buf_reset() by yourself |
	 * +-------------------------+---------------------------------------+-----------------------------------------------+
	 */
	assert(pool);
	buf_t *buf;
	litem_t *item = PI->freelist->tail;
	if (item) {
		buf = (buf_t*)item->data;
		PI->freelist->mv(BI->item, PI->freelist, PI->busylist, 0);
	} else {
		buf = __buf_new(pool, size);
		BI->item = PI->busylist->put(PI->busylist, (void*)buf, 0);
	}

	if (size > 0)
		__buf_data(buf, size);

	buf->offset = 0;
	buf->data[0] = '\0';
	if (++BI->ltimes < 0)
		BI->ltimes = 1;
	BI->utimes = 1;
	BI->ftimes = 0;
	BI->flag = BPF_LENT | (auto_reset > 0 ? BPF_AUTO : 0);
	return buf;
}

static int reset_cb(void *data, void *val)
{
	buf_t *buf = (buf_t*)data;
	if (BI->flag & BPF_AUTO)
		__buf_reset(buf, 0);
	return 1;
}

static void buf_pool_reset(buf_pool_t *pool)
{
	assert(pool);
	PI->busylist->apply(PI->busylist, &reset_cb, NULL);
}

static void buf_pool_dry(buf_pool_t *pool)
{
	assert(pool);
	list_free((void*)PI->freelist);
	PI->freelist = list_new(&buf_free);
}

static int buf_pool_busy_count(buf_pool_t *pool)
{
	assert(pool && PI->busylist);
	return PI->busylist->count(PI->busylist);
}

static int buf_pool_free_count(buf_pool_t *pool)
{
	assert(pool && PI->freelist);
	return PI->freelist->count(PI->freelist);
}

static int buf_pool_count(buf_pool_t *pool)
{
	assert(pool && PI->busylist && PI->freelist);
	return PI->busylist->count(PI->busylist) + PI->freelist->count(PI->freelist);
}

static int sum_cb(void *data, void *val)
{
	buf_t *buf = (buf_t*)data;
	long long *sum = (long long*)val;
	*sum += buf->size;
	return 1;
}

static long long buf_pool_busy_sum(buf_pool_t *pool)
{
	assert(pool && PI->busylist);
	long long sum = 0LL;
	PI->busylist->apply(PI->busylist, &sum_cb, &sum);
	return sum;
}

static long long buf_pool_free_sum(buf_pool_t *pool)
{
	assert(pool && PI->freelist);
	long long sum = 0LL;
	PI->freelist->apply(PI->freelist, &sum_cb, &sum);
	return sum;
}

static long long buf_pool_sum(buf_pool_t *pool)
{
	assert(pool && PI->busylist && PI->freelist);
	long long sum = 0LL;
	PI->freelist->apply(PI->freelist, &sum_cb, &sum);
	PI->busylist->apply(PI->busylist, &sum_cb, &sum);
	return sum;
}

static int max_cb(void *data, void *val)
{
	buf_t *buf = (buf_t*)data;
	unsigned int *max = (unsigned int*)val;
	if (buf->size > *max)
		*max = buf->size;
	return 1;
}

static unsigned int buf_pool_busy_max(buf_pool_t *pool)
{
	assert(pool && PI->busylist);
	unsigned int max = 0;
	PI->busylist->apply(PI->busylist, &max_cb, &max);
	return max;
}

static unsigned int buf_pool_free_max(buf_pool_t *pool)
{
	assert(pool && PI->freelist);
	unsigned int max = 0;
	PI->freelist->apply(PI->freelist, &max_cb, &max);
	return max;
}

static unsigned int buf_pool_max(buf_pool_t *pool)
{
	assert(pool && PI->freelist);
	unsigned int max = 0;
	PI->busylist->apply(PI->busylist, &max_cb, &max);
	PI->freelist->apply(PI->freelist, &max_cb, &max);
	return max;
}

buf_pool_t* buf_pool_new()
{
	buf_pool_t *pool = xcalloc(1, sizeof(buf_pool_t));
	pool->priv = xcalloc(1, sizeof(pool_inner_t));
	PI->busylist = list_new(&buf_free);
	PI->freelist = list_new(&buf_free);
	pool->lend = &buf_pool_lend;
	pool->reset = &buf_pool_reset;
	pool->dry = &buf_pool_dry;
	pool->busy_count = &buf_pool_busy_count;
	pool->free_count = &buf_pool_free_count;
	pool->count = &buf_pool_count;
	pool->busy_sum = &buf_pool_busy_sum;
	pool->free_sum = &buf_pool_free_sum;
	pool->sum = &buf_pool_sum;
	pool->busy_max = &buf_pool_busy_max;
	pool->free_max = &buf_pool_free_max;
	pool->max = &buf_pool_max;
	return pool;
}

void buf_pool_free(buf_pool_t *pool)
{
	assert(pool);
	list_free((void*)PI->busylist);
	list_free((void*)PI->freelist);
	free(pool->priv);
	free(pool);
}
