#include <sys/stat.h>
#include <sys/types.h>
#include <sys/time.h>
#include <sys/uio.h>
#include <pthread.h>
#include <assert.h>
#include <time.h>
#include <unistd.h>
#include <fcntl.h>
#include <stdarg.h>
#include <stdlib.h>
#include <string.h>
#include <stdio.h>
#include <errno.h>
#include "wrapper.h"
#include "misc.h"
#include "log.h"

#define LOGDIR			"log"
#define _L_(d)			LOGDIR"/"#d
#define LOG_COUNT		6
#define BUF_COUNT_MIN	100
#define LI				((log_inner_t*)log->priv)

typedef struct {
	int fd;
	char datestamp[12];
	const char *prefix;
	const char *dir;
	buf_t **buf;
	int buf_count;
	int buf_used;
	struct iovec *iov;
} log_inner_t;

static const char* datestamp(char buf[12])
{
	time_t t = time(NULL);
	struct tm tm;
	localtime_r(&t, &tm);
	#ifdef LOGFILE_HOUR
	sprintf(buf, "%04d%02d%02d%02d", tm.tm_year+1900, tm.tm_mon+1, tm.tm_mday, tm.tm_hour);
	#else
	sprintf(buf, "%04d%02d%02d", tm.tm_year+1900, tm.tm_mon+1, tm.tm_mday);
	#endif
	return buf;
}

static const char* timestamp(char buf[48], pid_t pid, pthread_t ppid)
{
	struct timeval t;
	gettimeofday(&t, NULL);
	struct tm tm;
	localtime_r(&t.tv_sec, &tm);
	sprintf(buf, "%02d:%02d:%02d,%06ld|%06d|%lx", tm.tm_hour, tm.tm_min, tm.tm_sec, t.tv_usec, pid, ppid);
	return buf;
}

static int __mkdir(const char *dir)
{
	if (mkdir(dir, 0731) < 0) {
		if (errno != EEXIST)
			return -1;
		struct stat s;
		if (stat(dir, &s) < 0 || !S_ISDIR(s.st_mode) || access(dir, W_OK) < 0)
			return -1;
	}
	return 0;
}

static void free_log_inner(log_inner_t *li)
{
	int i, j;
	log_inner_t *pl;
	for (i = 0; i < LOG_COUNT; i++) {
		pl = &li[i];
		if (pl->fd > 0)
			close(pl->fd);
		if (pl->buf) {
			for (j = 0; j < pl->buf_count; j++) {
				if (pl->buf[j])
					buf_return(pl->buf[j]);
			}
			free(pl->buf);
		}
		if (pl->iov)
			free(pl->iov);
	}
	free(li);
}

static log_inner_t* log_inner_new(const char *prefix, int buf_count, buf_pool_t *pool)
{
	log_inner_t *li = xcalloc(LOG_COUNT, sizeof(log_inner_t));
	li[0].dir = _L_(debug);
	li[0].buf_count = 1;
	li[1].dir = _L_(event);
	li[1].buf_count = buf_count;
	li[2].dir = _L_(warn);
	li[2].buf_count = 1;
	li[3].dir = _L_(error);
	li[3].buf_count = 1;
	li[4].dir = _L_(fatal);
	li[4].buf_count = 1;
	li[5].dir = _L_(tc);
	li[5].buf_count = 1;

	int i, j;
	log_inner_t *pl;
	for (i = 0; i < LOG_COUNT; i++) {
		pl = &li[i];
		if (__mkdir(pl->dir) < 0) {
			free_log_inner(li);
			return NULL;
		}
		pl->prefix = prefix;
		datestamp(pl->datestamp);
		pl->buf = xcalloc(pl->buf_count, sizeof(buf_t*));
		for (j = 0; j < pl->buf_count; j++)
			pl->buf[j] = pool->lend(pool, 128, 0);
	}

	pl = &li[1];
	if (pl->buf_count > 1)
		pl->iov = xcalloc(pl->buf_count, sizeof(struct iovec));

	return li;
}

static log_inner_t* log_inner_new_simple(const char *prefix)
{
	log_inner_t *li = xcalloc(LOG_COUNT, sizeof(log_inner_t));
	li[0].dir = _L_(debug);
	li[0].buf_count = 1;
	li[1].dir = _L_(event);
	li[1].buf_count = 1;
	li[2].dir = _L_(warn);
	li[2].buf_count = 1;
	li[3].dir = _L_(error);
	li[3].buf_count = 1;
	li[4].dir = _L_(fatal);
	li[4].buf_count = 1;
	li[5].dir = _L_(tc);
	li[5].buf_count = 1;

	int i, j;
	log_inner_t *pl;
	for (i = 0; i < LOG_COUNT; i++) {
		pl = &li[i];
		if (__mkdir(pl->dir) < 0) {
			free_log_inner(li);
			return NULL;
		}
		pl->prefix = prefix;
		datestamp(pl->datestamp);
		pl->buf = xcalloc(pl->buf_count, sizeof(buf_t*));
		for (j = 0; j < pl->buf_count; j++)
			pl->buf[j] = buf_new();
	}
	return li;
}

static int openfile(log_inner_t *pl)
{
	if (pl->fd > 0)
		close(pl->fd);
	char file[48];
	sprintf(file, "%s/%s_%s.log", pl->dir, pl->prefix, pl->datestamp);
	pl->fd = open(file, O_CREAT|O_APPEND|O_WRONLY, 0666);
	return pl->fd;
}

static void __sync(log_inner_t *pl)
{
	if (pl->fd <= 0)
		goto __out;
	int i;
	if (pl->iov) {
		for (i = 0; i < pl->buf_used; i++) {
			/* Note buf->data may be realloced. so don't fix iov_base before. */
			pl->iov[i].iov_base = pl->buf[i]->data;
			pl->iov[i].iov_len = pl->buf[i]->offset;
		}
		writev(pl->fd, pl->iov, pl->buf_used);
	} else {
		for (i = 0; i < pl->buf_used; i++)
			write(pl->fd, pl->buf[i]->data, pl->buf[i]->offset);
	}
__out:
	pl->buf_used = 0;
}

static void __log(log_inner_t *pl, const char *content, int len)
{
	char ds[12];
	if (strcmp(datestamp(ds), pl->datestamp)) {
		__sync(pl);
		memcpy(pl->datestamp, ds, sizeof(ds));
		openfile(pl);
	} else if (pl->fd <= 0)
		openfile(pl);

	buf_t *buf = pl->buf[pl->buf_used];
	/*
	 * Don't use buf_printf because the content maybe contains '%'.
	 * buf_data(buf, 64 + len);
	 * buf_printf(buf, "%s%c",	content, *(content+len-1)=='\n' ? ' ' : '\n');
	 */
	buf_reset(buf);
	buf_append(buf, content, len);
	if (*(content + len - 1) != '\n')
		buf_append(buf, "\n", 1);

	if (++pl->buf_used >= pl->buf_count)
		__sync(pl);
}

static void __log_sync(log_inner_t *li)
{
	int i;
	for (i = 0; i < LOG_COUNT; i++) {
		if (li[i].buf_used > 0)
			__sync(&li[i]);
	}
}

/* API */
log_t* log_new(const char *prefix, logtype_t *logbase, int buf_count, buf_pool_t *pool, int cbuf_size)
{
	assert(pool && logbase && cbuf_size > 64);
	if (__mkdir(LOGDIR) < 0)
		return NULL;
	if (!prefix)
		prefix = "";
	if (buf_count < BUF_COUNT_MIN)
		buf_count = BUF_COUNT_MIN;
	if (*logbase == LT_DEBUG)
		buf_count = 1;
	log_t *log = xcalloc(1, sizeof(log_t));
	log->logbase = logbase;
	log->cbuf = xmalloc(cbuf_size);
	log->cbuf_size = cbuf_size;
	log->priv = log_inner_new(prefix, buf_count, pool);
	return log;
}

log_t* log_new_simple(const char *prefix, logtype_t *logbase, int cbuf_size)
{
	assert(logbase && cbuf_size > 64);
	if (__mkdir(LOGDIR) < 0)
		return NULL;
	if (!prefix)
		prefix = "";
	log_t *log = xcalloc(1, sizeof(log_t));
	log->logbase = logbase;
	log->cbuf = xmalloc(cbuf_size);
	log->cbuf_size = cbuf_size;
	log->priv = log_inner_new_simple(prefix);
	return log;
}

void LOG(log_t *log, logtype_t type, const char *fmt, ...)
{
	if (type < *log->logbase)
		return;

	int fixtime = 1;
	log_inner_t *pl;
	switch (type) {
	case LT_DEBUG:
		pl = &LI[0];
		break;
	case LT_EVENT:
		fixtime = 0;
		pl = &LI[1];
		break;
	case LT_WARN:
		pl = &LI[2];
		break;
	case LT_ERROR:
		pl = &LI[3];
		break;
	case LT_FATAL:
		pl = &LI[4];
		break;
	case LT_TC:
		pl = &LI[5];
		break;
	default:
		pl = &LI[1];
	}

	if (fixtime) {
		char ts[48];
		int len = sprintf(log->cbuf, "[%s] ", timestamp(ts, PID, PPID));
		va_list v;
		va_start(v, fmt);
		len += vsnprintf(log->cbuf + len, log->cbuf_size - len, fmt, v);
		va_end(v);
		if (len >= log->cbuf_size)
			len = log->cbuf_size - 1;
		__log(pl, log->cbuf, len);
	} else {
		va_list v;
		va_start(v, fmt);
		int len = vsnprintf(log->cbuf, log->cbuf_size, fmt, v);
		va_end(v);
		if (len >= log->cbuf_size)
			len = log->cbuf_size - 1;
		__log(pl, log->cbuf, len);
	}
}

void DEBUG_S(log_t *log, const char *content)
{
	if (LT_DEBUG >= *log->logbase) {
		char ts[48];
		log_inner_t *pl = &LI[0];
		int len = sprintf(log->cbuf, "[%s] ", timestamp(ts, PID, PPID));
		sstrncpy(log->cbuf + len, content, log->cbuf_size - len);
		__log(pl, log->cbuf, strlen(log->cbuf));
	}
}

void DEBUG(log_t *log, const char *fmt, ...)
{
	if (LT_DEBUG >= *log->logbase) {
		char ts[48];
		log_inner_t *pl = &LI[0];
		int len = sprintf(log->cbuf, "[%s] ", timestamp(ts, PID, PPID));
		va_list v;
		va_start(v, fmt);
		len += vsnprintf(log->cbuf + len, log->cbuf_size - len, fmt, v);
		va_end(v);
		if (len >= log->cbuf_size)
			len = log->cbuf_size - 1;
		__log(pl, log->cbuf, len);
	}
}

void EVENT_S(log_t *log, const char *content)
{
	if (LT_EVENT >= *log->logbase) {
		char ts[48];
		log_inner_t *pl = &LI[1];
		int len = sprintf(log->cbuf, "[%s] ", timestamp(ts, PID, PPID));
		sstrncpy(log->cbuf + len, content, log->cbuf_size - len);
		__log(pl, log->cbuf, strlen(log->cbuf));
	}
}

void EVENT(log_t *log, const char *fmt, ...)
{
	if (LT_EVENT >= *log->logbase) {
		char ts[48];
		log_inner_t *pl = &LI[1];
		int len = sprintf(log->cbuf, "[%s] ", timestamp(ts, PID, PPID));
		va_list v;
		va_start(v, fmt);
		len += vsnprintf(log->cbuf + len, log->cbuf_size - len, fmt, v);
		va_end(v);
		if (len >= log->cbuf_size)
			len = log->cbuf_size - 1;
		__log(pl, log->cbuf, len);
	}
}

void WARN_S(log_t *log, const char *content)
{
	if (LT_WARN >= *log->logbase) {
		char ts[48];
		log_inner_t *pl = &LI[2];
		int len = sprintf(log->cbuf, "[%s] ", timestamp(ts, PID, PPID));
		sstrncpy(log->cbuf + len, content, log->cbuf_size - len);
		__log(pl, log->cbuf, strlen(log->cbuf));
	}
}

void WARN(log_t *log, const char *fmt, ...)
{
	if (LT_WARN >= *log->logbase) {
		char ts[48];
		log_inner_t *pl = &LI[2];
		int len = sprintf(log->cbuf, "[%s] ", timestamp(ts, PID, PPID));
		va_list v;
		va_start(v, fmt);
		len += vsnprintf(log->cbuf + len, log->cbuf_size - len, fmt, v);
		va_end(v);
		if (len >= log->cbuf_size)
			len = log->cbuf_size - 1;
		__log(pl, log->cbuf, len);
	}
}

void ERROR_S(log_t *log, const char *content)
{
	if (LT_ERROR >= *log->logbase) {
		char ts[48];
		log_inner_t *pl = &LI[3];
		int len = sprintf(log->cbuf, "[%s] ", timestamp(ts, PID, PPID));
		sstrncpy(log->cbuf + len, content, log->cbuf_size - len);
		__log(pl, log->cbuf, strlen(log->cbuf));
	}
}

void ERROR(log_t *log, const char *fmt, ...)
{
	if (LT_ERROR >= *log->logbase) {
		char ts[48];
		log_inner_t *pl = &LI[3];
		int len = sprintf(log->cbuf, "[%s] ", timestamp(ts, PID, PPID));
		va_list v;
		va_start(v, fmt);
		len += vsnprintf(log->cbuf + len, log->cbuf_size - len, fmt, v);
		va_end(v);
		if (len >= log->cbuf_size)
			len = log->cbuf_size - 1;
		__log(pl, log->cbuf, len);
	}
}

void FATAL_S(log_t *log, const char *content)
{
	if (LT_FATAL >= *log->logbase) {
		char ts[48];
		log_inner_t *pl = &LI[4];
		int len = sprintf(log->cbuf, "[%s] ", timestamp(ts, PID, PPID));
		sstrncpy(log->cbuf + len, content, log->cbuf_size - len);
		__log(pl, log->cbuf, strlen(log->cbuf));
	}
}

void FATAL(log_t *log, const char *fmt, ...)
{
	if (LT_FATAL >= *log->logbase) {
		char ts[48];
		log_inner_t *pl = &LI[4];
		int len = sprintf(log->cbuf, "[%s] ", timestamp(ts, PID, PPID));
		va_list v;
		va_start(v, fmt);
		len += vsnprintf(log->cbuf + len, log->cbuf_size - len, fmt, v);
		va_end(v);
		if (len >= log->cbuf_size)
			len = log->cbuf_size - 1;
		__log(pl, log->cbuf, len);
	}
}

void TC_S(log_t *log, const char *content)
{
	if (1) {
		char ts[48];
		log_inner_t *pl = &LI[5];
		int len = sprintf(log->cbuf, "[%s] ", timestamp(ts, PID, PPID));
		sstrncpy(log->cbuf + len, content, log->cbuf_size - len);
		__log(pl, log->cbuf, strlen(log->cbuf));
	}
}

void TC(log_t *log, const char *fmt, ...)
{
	if (1) {
		char ts[48];
		log_inner_t *pl = &LI[5];
		int len = sprintf(log->cbuf, "[%s] ", timestamp(ts, PID, PPID));
		va_list v;
		va_start(v, fmt);
		len += vsnprintf(log->cbuf + len, log->cbuf_size - len, fmt, v);
		va_end(v);
		if (len >= log->cbuf_size)
			len = log->cbuf_size - 1;
		__log(pl, log->cbuf, len);
	}
}

void log_sync(log_t *log)
{
	assert(log);
	if (log->priv)
		__log_sync(LI);
}

void log_free(void *val)
{
	log_t *log = (log_t*)val;
	if (log->priv) {
		__log_sync(LI);
		free_log_inner(LI);
	}
	if (log->cbuf)
		free(log->cbuf);
	free(log);
}
