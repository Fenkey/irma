#ifndef __LOG_H__
#define __LOG_H__

#include "buf.h"

#ifdef __cplusplus
extern "C" {
#endif

#define PID		getpid()
#define PPID	pthread_self()

typedef enum {
	LT_DEBUG = 0,
	LT_EVENT,
	LT_WARN,
	LT_ERROR,
	LT_FATAL,
	LT_TC,
} logtype_t;

typedef struct {
	logtype_t	*logbase;
	char		*cbuf;
	int			cbuf_size;
	void		*priv;
} log_t;

log_t* log_new(const char *prefix, logtype_t *logbase, int buf_count, buf_pool_t *pool, int cbuf_size);
log_t* log_new_simple(const char *prefix, logtype_t *logbase, int cbuf_size);
void log_sync(log_t *log);
void log_free(void *log);

void LOG(log_t *log, logtype_t type, const char *fmt, ...);
void DEBUG_S(log_t *log, const char *content);
void DEBUG(log_t *log, const char *fmt, ...);
void EVENT_S(log_t *log, const char *content);
void EVENT(log_t *log, const char *fmt, ...);
void WARN_S(log_t *log, const char *content);
void WARN(log_t *log, const char *fmt, ...);
void ERROR_S(log_t *log, const char *content);
void ERROR(log_t *log, const char *fmt, ...);
void FATAL_S(log_t *log, const char *content);
void FATAL(log_t *log, const char *fmt, ...);
void TC_S(log_t *log, const char *content);
void TC(log_t *log, const char *fmt, ...);

#ifdef __cplusplus
}
#endif

#endif
