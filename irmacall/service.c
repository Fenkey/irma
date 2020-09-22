#include <unistd.h>
#include <pthread.h>
#include <signal.h>
#include <string.h>
#include <stdarg.h>
#include <stdlib.h>
#include <stdio.h>
#include "fcgiapp.h"
#include "fcgi_stdio.h"
#include "wrapper.h"
#include "param.h"
#include "fetcher.h"
#include "form_param.h"
#include "list.h"
#include "map.h"
#include "misc.h"
#include "fuse.h"
#include "service.h"

#define BMAX_DEFAULT		6*1024*1024
#define LOGBUF_COUNT_MAX	500
#define LOGBASE_ST_MAX		3600
#define SPIRIT_INTERVAL		5
#define GC_INTERVAL			600
#define MOCK_HEADER			"HTTP_IRMA_MOCK"

#define BOOT_READY() \
do { \
	__main_booting = 1; \
} while (0)

#define BOOT_WAIT() \
do { \
	pthread_mutex_lock(&__boot_mutex); \
	while (__main_booting > 0) \
	pthread_cond_wait(&__boot_cond, &__boot_mutex); \
	pthread_mutex_unlock(&__boot_mutex); \
} while (0)

#define BOOT_DONE(v) \
do { \
	pthread_mutex_lock(&__boot_mutex); \
	__main_booting = v; \
	pthread_cond_signal(&__boot_cond); \
	pthread_mutex_unlock(&__boot_mutex); \
} while (0)

#define RELOAD_READY() \
do { \
	__thread_reloaded = 0; \
	__main_reloading = 1; \
} while (0)

#define RELOAD_FAIL() \
do { \
	__main_reloading = 0; \
} while (0)

#define RELOAD_WAIT() \
do { \
	pthread_mutex_lock(&__reload_mutex); \
	while (__main_reloading > 0) \
	pthread_cond_wait(&__reload_cond, &__reload_mutex); \
	pthread_mutex_unlock(&__reload_mutex); \
} while (0)

#define RELOAD_DONE() \
do { \
	pthread_mutex_lock(&__reload_mutex); \
	__main_reloading = 0; \
	pthread_cond_signal(&__reload_cond); \
	pthread_mutex_unlock(&__reload_mutex); \
} while (0)

#define RELOAD_COUNT() \
do { \
	pthread_mutex_lock(&__reload_count_mutex); \
	__thread_reloaded++; \
	pthread_mutex_unlock(&__reload_count_mutex); \
} while (0)

#define EXIT_COUNT() \
do { \
	pthread_mutex_lock(&__exit_count_mutex); \
	__thread_exited++; \
	pthread_mutex_unlock(&__exit_count_mutex); \
} while (0)

#define REPORT_URL() \
do { \
	pthread_mutex_lock(&__report_mutex); \
	spirit_report(&report_url); \
	pthread_mutex_unlock(&__report_mutex); \
} while (0)

#define REPORT_CONSOLE() \
do { \
	pthread_mutex_lock(&__report_mutex); \
	spirit_report(&report_console); \
	pthread_mutex_unlock(&__report_mutex); \
} while (0)

#define WI ((worker_inner_t*)(w->priv_sys))

typedef struct __rescode rescode_t;
static struct __rescode {
	int code;
	const char *desc;
} __codelist[] = {
	{100, "Continue"},
	{101, "Switching Protocols"},
	{102, "Processing"},
	{103, "Early Hints"},
	{200, "OK"},
	{201, "Created"},
	{202, "Accepted"},
	{203, "Non-Authoritative Information"},
	{204, "No Content"},
	{205, "Reset Content"},
	{206, "Partial Content"},
	{207, "Multi-Status"},
	{208, "Already Reported"},
	{226, "IM Used"},
	{300, "Multiple Choices"},
	{301, "Moved Permanently"},
	{302, "Moved Temporarily"},
	{303, "See Other"},
	{304, "Not Modified"},
	{305, "Use Proxy"},
	{307, "Temporary Redirect"},
	{308, "Permanent Redirect"},
	{400, "Bad Request"},
	{401, "Unauthorized"},
	{402, "Payment Required"},
	{403, "Forbidden"},
	{404, "Not Found"},
	{405, "Method Not Allowed"},
	{406, "Not Acceptable"},
	{407, "Proxy Authentication Required"},
	{408, "Request Timeout"},
	{409, "Conflict"},
	{410, "Gone"},
	{411, "Length Required"},
	{412, "Precondition Failed"},
	{413, "Request Entity Too Large"},
	{414, "Request-URI Too Large"},
	{415, "Unsupported Media Type"},
	{416, "Requested Range Not Satisfiable"},
	{417, "Expectation Failed"},
	{421, "Misdirected Request"},
	{422, "Unprocessable Entity"},
	{423, "Locked"},
	{424, "Failed Dependency"},
	{425, "Too Early"},
	{426, "Upgrade Required"},
	{428, "Precodition Required"},
	{429, "Too Many Requests"},
	{431, "Request Header Fields Too Large"},
	{451, "Unavailable For Legal Reasons"},
	{500, "Internal Server Error"},
	{501, "Not Implemented"},
	{502, "Bad Gateway"},
	{503, "Service Unavailable"},
	{504, "Gateway Timeout"},
	{505, "HTTP Version Not Supported"},
	{506, "Variant Also Negotiates"},
	{507, "Insufficient Storage"},
	{508, "Loop Detected"},
	{510, "Not Extended"},
	{511, "Network Authentication Required"},
	{-1, ""},
};

static pid_t __pid;
static service_t *__s = NULL;
static logtype_t __logbase_ori;
static time_t __logbase_st = 0L;
static list_t *__workers = NULL;
static int __thread_reloaded = 0;
static int __thread_exited = 0;
static pthread_key_t __thread_key;
static pthread_once_t __thread_once = PTHREAD_ONCE_INIT;
static pthread_mutex_t __boot_mutex = PTHREAD_MUTEX_INITIALIZER;
static pthread_cond_t __boot_cond = PTHREAD_COND_INITIALIZER;
static pthread_mutex_t __reload_mutex = PTHREAD_MUTEX_INITIALIZER;
static pthread_cond_t __reload_cond = PTHREAD_COND_INITIALIZER;
static pthread_mutex_t __reload_count_mutex = PTHREAD_MUTEX_INITIALIZER;
static pthread_mutex_t __exit_count_mutex = PTHREAD_MUTEX_INITIALIZER;
static pthread_mutex_t __report_mutex = PTHREAD_MUTEX_INITIALIZER;
static int __main_booting = 0;
static int __main_reloading = 0;

/* Global */
log_t *g_log = NULL;
buf_pool_t *g_buf_pool = NULL;

typedef struct {
	litem_t *this;
	buf_t *app;
	buf_t *ver;
	long bmax;

	buf_t *spirit_url;

	volatile int busy;
	buf_t *buf;
	buf_t *req_body;
	char cbuf[5120];
	const char *method;
	const char *request_uri;
	const char *query_string;
	const char *content_type;
	paramparser_t *paramparser;
	fparamparser_t *fparamparser;
	paramlist_t *req_header_param;
	paramlist_t *res_header_param;
	paramlist_t *get_param;
	paramlist_t *post_param;
	fparamlist_t *form_param;
	list_t *req_mock_param;
	char **req_mockenvp;
	int req_ismock;
	int res_finished;
	FCGX_Request req;
	int logsync;
	int reload_done;
	int (*cb)(worker_t*);

	long req_times;
	long res_times;
	long error_times;
	long fatal_times;
	time_t req_lasttime;
	time_t error_lasttime;
	time_t fatal_lasttime;

	map_t *fuse_map;
	fuse_t *fuse;
} worker_inner_t;

typedef struct {
	pthread_t ppid;
	int today;
	char host[64];
	long bmax;
	buf_t *buf;
	buf_t *app;
	buf_t *ver;
	buf_t *url;
	time_t gc_lasttime;
	fetcher_t *fetcher;
	char start_time[20];
	logtype_t logbase;
	log_t *log;
} spirit_t;
static spirit_t __spirit = { .ppid = 0, .fetcher = NULL };

static void* worker_run(void*);
static void worker_reset(worker_t*);
static void worker_free(void*);
static void report_url();
static void report_console();
static void spirit_report(void (*)(const buf_t*));

static int g_log_build()
{
	g_buf_pool = buf_pool_new();
	g_log = log_new_simple(&__s->logbase, 2048);
	return g_log != NULL;
}

static void inner_params_new(worker_inner_t *wi, buf_pool_t *pool)
{
	wi->paramparser = paramparser_new(pool);
	wi->fparamparser = fparamparser_new(pool);

	wi->req_header_param = wi->paramparser->paramlist_new(wi->paramparser);
	wi->res_header_param = wi->paramparser->paramlist_new(wi->paramparser);
	wi->get_param = wi->paramparser->paramlist_new(wi->paramparser);
	wi->post_param = wi->paramparser->paramlist_new(wi->paramparser);
	wi->form_param = wi->fparamparser->fparamlist_new(wi->fparamparser);
	if (__s->mock_support)
		wi->req_mock_param = list_new(&free);
}

static void inner_params_free(worker_inner_t *wi)
{
	if (wi->req_header_param)
		wi->paramparser->paramlist_free(wi->req_header_param);
	if (wi->res_header_param)
		wi->paramparser->paramlist_free(wi->res_header_param);
	if (wi->get_param)
		wi->paramparser->paramlist_free(wi->get_param);
	if (wi->post_param)
		wi->paramparser->paramlist_free(wi->post_param);
	if (wi->form_param)
		wi->fparamparser->fparamlist_free(wi->form_param);
	if (wi->req_mock_param)
		list_free(wi->req_mock_param);
	if (wi->req_mockenvp)
		free(wi->req_mockenvp);

	paramparser_free(wi->paramparser, 0);
	fparamparser_free(wi->fparamparser, 0);
}

static worker_inner_t* worker_inner_new(buf_pool_t *pool, litem_t *this)
{
	worker_inner_t *wi = xcalloc(1, sizeof(*wi));
	wi->this = this;
	wi->app = pool->lend(pool, 0, 0);
	wi->ver = pool->lend(pool, 0, 0);
	wi->bmax = BMAX_DEFAULT;
	wi->busy = -1;
	wi->spirit_url = pool->lend(pool, 0, 0);
	wi->buf = pool->lend(pool, 0, 1);
	wi->req_body = pool->lend(pool, 128, 1);
	wi->fuse_map = map_new();
	inner_params_new(wi, pool);
	FCGX_InitRequest(&wi->req, 0, 0);
	return wi;
}

static void worker_inner_free(worker_inner_t *wi)
{
	inner_params_free(wi);
	FCGX_Free(&wi->req, 1);
	map_free(wi->fuse_map);
	free(wi);
}

static void app_free(worker_t *w, int finalize)
{
	finalize |= (WI->busy >= 0);
	__s->sopt->app_free(w, finalize);
	WI->busy = -1;
}

static const char* check_rescode(int code)
{
	static const char *desc = "Unassigned";
	static int inx[] = { -1, 0, 4, 14, 22, 50 };
	rescode_t *p = &__codelist[inx[code/100]];
	do {
		if (p->code == code) {
			desc = p->desc;
			break;
		} else if (p->code > code)
			break;
	} while ((++p)->code > 0)
		;
	return desc;
}

static void __free(int common_free)
{
	if (__workers)
		list_free(__workers);
	if (common_free) {
		if (__s->sopt->common_free)
			__s->sopt->common_free(__s->thread_count);
		irmacurl_global_free();
	}
	if (__spirit.fetcher)
		fetcher_free(__spirit.fetcher);
	if (__spirit.log)
		log_free(__spirit.log);
	if (g_log)
		log_free(g_log);
	if (g_buf_pool)
		buf_pool_free(g_buf_pool);
}

static int worker_logsync(void *data, void *val)
{
	worker_t *w = (worker_t*)data;
	if (w->ppid > 0) {
		WI->logsync = *(int*)val;
		pthread_kill(w->ppid, SIGUSR2);
	}
	return 1;
}

static int worker_pause(void *data, void *val)
{
	worker_t *w = (worker_t*)data;
	if (w->ppid > 0) {
		pthread_join(w->ppid, NULL);
		w->ppid = 0;
	}
	return 1;
}

static void suspend()
{
	__workers->apply(__workers, &worker_pause, NULL);
	if (__spirit.ppid > 0)
		pthread_join(__spirit.ppid, NULL);
}

static int worker_exit(void *data, void *val)
{
	worker_t *w = (worker_t*)data;
	if (w->ppid > 0)
		pthread_kill(w->ppid, SIGUSR1);
	return 1;
}

static void __exit()
{
	__workers->apply(__workers, &worker_exit, NULL);
	if (__spirit.ppid > 0)
		pthread_kill(__spirit.ppid, SIGUSR1);
}

static int console_printf(const char *fmt, ...)
{
	char buf[2048];
	va_list v;
	va_start(v, fmt);
	vsnprintf(buf, sizeof(buf), fmt, v);
	va_end(v);

	#if defined(fprintf)
	#undef stderr
	#undef fprintf
	#endif
	fprintf(stderr, "%s", buf);
	return 0;
}

static int workers_reload()
{
	/* Reload the workers iteratively. */
	worker_t *w = __workers->tail->data;
	if (w->ppid <= 0)
		return 0;
	return pthread_kill(w->ppid, SIGUSR2) == 0;
}

static int main_reload()
{
	if (__main_booting || __main_reloading)
		return 0;
	EVENT(g_log, "Core - Reload begin ...");
	RELOAD_READY();
	if (__s->sopt->common_reload && __s->sopt->common_reload(__s->thread_count) < 0)
		return -1;
	if (!workers_reload()) {
		RELOAD_FAIL();
		EVENT(g_log, "Core - Fail to reload");
		return 0;
	}
	__s->reload_times++;
	time(&__s->reload_lasttime);
	RELOAD_WAIT();
	EVENT(g_log, "Core - Total (%d) workers have performed the reload", __thread_reloaded);
	return 0;
}

static void worker_pool_dry(worker_t *w)
{
	//w->pool->count(w->pool);
	inner_params_free(WI);
	w->pool->dry(w->pool);
	//w->pool->count(w->pool);
	inner_params_new(WI, w->pool);
	//w->pool->count(w->pool);
}

static int thread_logsync1(worker_t *w)
{
	log_sync(w->log);
	WI->logsync = 0;
	return 0;
}

static int thread_logsync2(worker_t *w)
{
	worker_pool_dry(w);
	log_sync(w->log);
	WI->logsync = 0;
	return 0;
}

static int thread_reload(worker_t *w)
{
	if (!__s->sopt->app_reload)
		return 0;
	WI->busy = -1;
	worker_pool_dry(w);
	log_sync(w->log);
	if ((WI->busy = __s->sopt->app_reload(w)) == 0)
		RELOAD_COUNT();
	if (WI->reload_done) {
		WI->reload_done = 0;
		RELOAD_DONE();
	}
	return WI->busy;
}

static void sig_handle_main(int signo)
{
	switch (signo) {
	case SIGTERM:
		DEBUG(g_log, "Core - Caught signal of %d to exit now", signo);
		__exit();
		break;

	case SIGHUP:
		if (main_reload() < 0) {
			ERROR(g_log, "Core - Fail to reload and exit now");
			__exit();
		}
		break;

	case SIGINT:
		/*
		 * The switch of __s->logbase only affect the output of DEBUG logs. It's nothing
		 * with the other types logs. Especially the EVENT logs are still cached as same
		 * way as before.
		 */
		if (__s->logbase != LT_DEBUG) {
			if (__logbase_ori == LT_EVENT) {
				int ls = 1;
				__workers->apply(__workers, &worker_logsync, (void*)&ls);
			}
			__s->logbase = LT_DEBUG;
			__logbase_st = time(NULL);
			DEBUG(g_log, "Core - Caught signal of %d to change logbase to DEBUG", signo);
		} else if (__logbase_ori != __s->logbase) {
			DEBUG(g_log, "Core - Caught signal of %d to change logbase to ORI", signo);
			__s->logbase = __logbase_ori;
			__logbase_st = time(NULL);
		}
		break;

	case 64: /* SIGRTMAX, which is a macro. */
		if (!__main_booting && !__main_reloading && __spirit.ppid > 0)
			pthread_kill(__spirit.ppid, SIGUSR2);
		break;

	default:
		DEBUG(g_log, "Core - Caught signal of %d", signo);
	}
}

static void sig_handle_thread(int signo)
{
	worker_t *w = CURRENT;
	switch (signo) {
	case SIGUSR1:
		if (w) {
			app_free(w, 0);
			EXIT_COUNT();
		}
		pthread_exit(NULL);
		break;

	case SIGUSR2:
		if (!w) { /* spirit */
			REPORT_CONSOLE();
			return;
		}
		/* It's worker */
		if (WI->busy < 0)
			return;
		int (*f)(worker_t*) = !WI->logsync ? thread_reload : (WI->logsync == 1 ? thread_logsync1 : thread_logsync2);
		if (WI->busy > 0) {
			WI->cb = f;
			if (__s->sopt->app_is_busy)
				__s->sopt->app_is_busy(w);
			return;
		}
		if (f(w) < 0) {
			app_free(w, 1);
			EXIT_COUNT();
			FATAL(w->log, "Core - Worker(%d) exit because reload or logsync failed. Total (%d) workers exited so far", w->index, __thread_exited);
			pthread_exit(NULL);
		}
		break;
	}
}

static void sig_install_main()
{
	/*
	 * Mono consumes a set of signals during execution that your applications will not be able to consume, here is what these are:
	 * SIGPWR, SIGXCPU: these are used internally by the GC and pthreads.
	 * SIGFPE: caught so we can turn that into an exception
	 * SIGQUIT, SIGKILL to produce ExecutionEngineException
	 * SIGSEGV: to produce NullReferenceExceptions
	 * SIGCHLD: to track the life-cycle of processes (notably System.Diagnostics.Process)
	 */
	struct sigaction act;
	memset(&act, 0, sizeof(struct sigaction));
	act.sa_handler = sig_handle_main;
	act.sa_flags = SA_RESTART;
	sigemptyset (&act.sa_mask);

	int i = 1;
	for (; i < 32; i++) {
		switch (i) {
		case SIGPWR:
		case SIGXCPU:
		case SIGFPE:
		case SIGQUIT:
		case SIGSEGV:
		case SIGCHLD:
			continue;
		default:
			sigaction(i, &act, NULL);
		}
	}
	#if defined(SIGRTMIN) && defined(SIGRTMAX)
	/* Mono's runtime will use some RT signals for itself so we have to avoid these. */
	for (i = SIGRTMIN+4; i < SIGRTMAX+1; i++)
		sigaction(i, &act, NULL);
	#endif
}

static void sig_install_worker()
{
	struct sigaction act;
	memset(&act, 0, sizeof(struct sigaction));
	act.sa_handler = sig_handle_thread;
	act.sa_flags = SA_RESTART;
	sigemptyset (&act.sa_mask);
	sigaction(SIGUSR1, &act, NULL);
	sigaction(SIGUSR2, &act, NULL);
}

static void launch_info(worker_t *w, const char *app, const char *ver, long bmax, const char *url)
{
	if (WI->busy < 0) {
		buf_printf(WI->app, app ? app : "");
		buf_printf(WI->ver, ver ? ver : "");
		if (url) {
			/* Don't use buf_printf because the url maybe contains '%'. */
			buf_reset(WI->spirit_url);
			buf_append(WI->spirit_url, url, strlen(url));
		}
		if (bmax > 0)
			WI->bmax = bmax;
	}
}

static void boot_next(worker_t *w)
{
	litem_t *p = WI->this->link;
	if (p == __workers->tail) {
		BOOT_DONE(0);
		return;
	}
	w = p->data;
	if (pthread_create(&w->ppid, 0, &worker_run, (void*)w) != 0)
		BOOT_DONE(-1);
}

static void reload_next(worker_t *w)
{
	litem_t *p;
	worker_t *curr = w;
	for (;;) {
		p = WI->this->link;
		if (p == __workers->tail)
			break;
		w = p->data;
		if (w->ppid > 0 && WI->busy >= 0 && !pthread_kill(w->ppid, SIGUSR2))
			return;
	}
	/*
	 * The current worker is in charge of finishing the RELOAD_WAIT. But it's
	 * a bit different from booting case that we can't invoke RELOAD_DONE here due
	 * to the entire reload action is not yet finished.
	 */
	w = curr;
	WI->reload_done = 1;
}

static void launched(worker_t *w)
{
	if (WI->busy < 0) {
		WI->busy = 0;
		void (*f)(worker_t*) = __main_reloading ? reload_next : boot_next;
		(*f)(w);
	}
}

static void once_over(worker_t *w)
{
	if (WI->busy > 0) {
		FCGX_Finish_r(&WI->req);
		if (WI->fuse)
			fuse_evaluate_out(WI->fuse, WI->req_lasttime, WI->error_times, WI->fatal_times);
		WI->busy = 0;
	}
}

static void log_record(worker_t *w, logtype_t type)
{
	switch (type) {
	case LT_ERROR:
		WI->error_times++;
		WI->error_lasttime = time(NULL);
		break;
	case LT_FATAL:
		WI->fatal_times++;
		WI->fatal_lasttime = time(NULL);
		break;
	default:
		break;
	}
}

static int fuse_check(worker_t *w, const char *handler)
{
	WI->fuse = WI->fuse_map->get(WI->fuse_map, handler);
	if (!WI->fuse) {
		WI->fuse = xcalloc(1, sizeof(fuse_t));
		WI->fuse_map->set(WI->fuse_map, handler, WI->fuse, &free);
	}
	if (fuse_evaluate_in(WI->fuse, WI->req_lasttime, WI->error_times, WI->fatal_times) < 0) {
		w->send_http(w, 403, NULL);
		FATAL(w->log, "Core - Request is forbidden due to fused on: '%s'. Blown times are (%d) in a valid checking period", handler, WI->fuse->fuse_times);
		return -1;
	}
	return 0;
}

static char** __envp(worker_t *w)
{
	if (__s->mock_support && WI->req_ismock) {
		if (!WI->req_mockenvp)
			WI->req_mockenvp = (char**)WI->req_mock_param->toarray(WI->req_mock_param, NULL);
		return WI->req_mockenvp;
	}
	return WI->req.envp;
}

static int request_mock_parse(worker_t *w)
{
	if (!WI->req_body->data || WI->req_body->offset <= 0)
		return -1;
	const char *rnrn = memstr(WI->req_body->data, WI->req_body->offset, "\r\n\r\n", 4);
	if (!rnrn)
		return -1;

	char *p = WI->req_body->data;
	do {
		char *rn = strstr(p, "\r\n");
		if (rn == p || rn > rnrn)
			break;
		buf_reset(WI->buf);
		buf_append(WI->buf, p, rn - p);
		WI->req_mock_param->put(WI->req_mock_param, (void*)xstrdup(WI->buf->data), 0L);
		p = rn + 2;
	} while (*p && p < rnrn)
		;
	WI->req_mock_param->put(WI->req_mock_param, NULL, 0L);

	WI->req_ismock = 1;
	char **envp = __envp(w);
	if (!(WI->method = FCGX_GetParam("REQUEST_METHOD", envp)))
		return -1;
	if (!(WI->request_uri = FCGX_GetParam("REQUEST_URI", envp)))
		return -1;
	WI->query_string = FCGX_GetParam("QUERY_STRING", envp);
	WI->content_type = FCGX_GetParam("CONTENT_TYPE", envp);

	if (!strcasecmp(WI->method, "POST") || !strcasecmp(WI->method, "PUT")) {
		p = WI->req_body->data;
		int len = rnrn - p + 4;
		if (!*(p + len))
			return -1;
		if (WI->req_body->offset - len > WI->bmax) {
			buf_force_reset(WI->req_body);
			goto __413;
		}
		WI->req_body->data = memmove(p, p + len, WI->req_body->offset - len);
		WI->req_body->offset -= len;
	}
	DEBUG(w->log, "Core - Mock request found");
	return 0;

__413:
	/*
	 * A request with big size body can be received just when the size is under
	 * the both limit of 'client_max_body_size' in nginx and bmax in irma.
	 */
	w->send_http(w, 413, NULL);
	WARN(w->log, "Core - Mock request: Reject request due to the size is out of service limit");
	return -1;
}

static int request_basic_parse(worker_t *w)
{
	char **envp = __envp(w);
	if (!(WI->method = FCGX_GetParam("REQUEST_METHOD", envp)))
		return -1;
	if (!(WI->request_uri = FCGX_GetParam("REQUEST_URI", envp)))
		return -1;
	if (!strcasecmp(WI->method, "POST") || !strcasecmp(WI->method, "PUT")) {
		int n, len = 0, size = sizeof(WI->cbuf);
		char *p = FCGX_GetParam("CONTENT_LENGTH", envp);
		if (p && (len = atoi(p)) > WI->bmax)
			goto __413;
		if (len > 0)
			buf_data(WI->req_body, len);
		/*
		 * We ignore the case of len <= 0 and try to extract the body according
		 * to the actual situation. However, if the len is effective, we would
		 * compare whether the actual length is consistent eventually.
		 */
		do {
			if ((n = FCGX_GetStr(WI->cbuf, size, WI->req.in)) > 0) {
				if (WI->req_body->offset + n > WI->bmax) {
					buf_force_reset(WI->req_body);
					goto __413;
				}
				buf_append(WI->req_body, WI->cbuf, n);
			}
		} while (n >= size)
			;
		if (WI->req_body->offset <= 0) {
			/* It's up to the higher irmakit. */
			// return -1;
		} else if (len > 0 && WI->req_body->offset != len)
			goto __400;
	}
	WI->query_string = FCGX_GetParam("QUERY_STRING", envp);
	WI->content_type = FCGX_GetParam("CONTENT_TYPE", envp);

	/*
	 * The preconditions on supporting mock:
	 * 1. '-k' set in irma-launch script, which will cause __s->mock_support = 1
	 * 2. POST the dumped request body by client.
	 * 3. POST with header：HTTP_IRMA_MOCK:xxx
	 *
	 * For example, you might post 1.dump file that was generated by request_dump (IRMAKit.IRequest.ReqDump) to re-present:
	 * curl --data-binary "@/home/fenkey/tmp/1.dump" -H"IRMA-MOCK:1" http://localhost:8020/Foo/request_params?....
	 */
	if (__s->mock_support && FCGX_GetParam(MOCK_HEADER, envp) && request_mock_parse(w) < 0)
		return -1;
	return 0;

__400:
	w->send_http(w, 400, NULL);
	WARN(w->log, "Core - Bad request, of which the 'Content-Length' is wrong");
	return -1;

__413:
	/*
	 * A request with big size body can be received just when the size is under
	 * the both limit of 'client_max_body_size' in nginx and bmax in irma.
	 */
	w->send_http(w, 413, NULL);
	WARN(w->log, "Core - Reject request due to the size is out of service limit");
	return -1;
}

/*
 * Returns:
 * -1: Break out the Handler() in irmakit to end. refer to irmakit/Web/Service.cs
 *  0: Invalid return from CaptureEvent() of irmakit for waiting the next access.
 *  1: Valid return from CaptureEvent() of irmakit for handling the current request.
 */
static int request_accept(worker_t *w)
{
	if (WI->busy != 0)
		return 0;
	worker_reset(w);

	int ret;
	if (WI->cb) {
		ret = WI->cb(w);
		WI->cb = NULL;
		if (ret < 0)
			return -1;
	}

	/* Refer to source of fcgi, it should exit. */
	if ((ret = FCGX_Accept_r(&WI->req)) < 0) {
		ERROR(w->log, "Core - Fail to accept request (ret:%d)", ret);
		return -1;
	}
	/* busy will be -1 if reload is doing or was failed. */
	if (WI->busy < 0)
		return 0;
	WI->busy = 1;
	ret = request_basic_parse(w);
	if (++WI->req_times < 0)
		WI->req_times = 1;
	WI->req_lasttime = time(NULL);
	if (ret < 0) {
		once_over(w);
		return 0;
	}
	return 1;
}

static int request_headers_parse(worker_t *w)
{
	int count = WI->req_header_param->count(WI->req_header_param);
	if (count > 0)
		return count;
	char **p = __envp(w);
	if (p && *p) {
		for (; *p; p++) {
			//DEBUG(w->log, *p);
			if (strncasecmp(*p, "HTTP_", 5))
				continue;
			char *c = strchr(*p, '=');
			if (!c)
				continue;
			param_t *h = WI->req_header_param->ext(WI->req_header_param);
			buf_append(h->key, *p, c - *p);
			buf_printf(h->value, "%s", *(c + 1) ? c + 1 : "");
		}
		count = WI->req_header_param->count(WI->req_header_param);
	}
	return count;
}

static void param_decode(param_t *p, buf_t *buf)
{
	if (p && p->value->offset > 0) {
		url_decode(buf, p->value->data);
		buf_copy(buf, p->value);
	}
}

static int request_get_parse(worker_t *w)
{
	if (!WI->query_string)
		return 0;
	int count = WI->get_param->count(WI->get_param);
	if (count <= 0) {
		paramparser_t *parser = WI->paramparser;
		count = parser->parse(WI->get_param, WI->query_string, 0, '&', '=', &param_decode);
	}
	return count;
}

static int request_post_parse(worker_t *w, int *file_count)
{
	if (WI->req_body->offset <= 0 || !WI->content_type)
		return 0;

	int count = WI->post_param->count(WI->post_param) + WI->form_param->post_count(WI->form_param);
	int fcount = WI->form_param->file_count(WI->form_param);
	if ((count += fcount) > 0)
		goto __out;

	if (!strncasecmp(WI->content_type, "application/x-www-form-urlencoded", 33)) {
		paramparser_t *parser = WI->paramparser;
		count = parser->parse(WI->post_param, WI->req_body->data, WI->req_body->offset, '&', '=', &param_decode);
	} else if (!strncasecmp(WI->content_type, "multipart/form-data", 19)) {
		char *boundary = strcasestr(WI->content_type, "boundary=");
		if (boundary) {
			fparamparser_t *parser = WI->fparamparser;
			count = parser->parse(WI->form_param, WI->req_body, boundary + 9, 0, &fcount);
		}
	}
__out:
	if (file_count)
		*file_count = fcount;
	return count;
}

static char** fcgi_params(worker_t *w)
{
	return __envp(w);
}

static const char* get_fcgi_param(worker_t *w, const char *param_name)
{
	return FCGX_GetParam(param_name, __envp(w));
}

static const char* request_method(worker_t *w)
{
	return WI->method;
}

static const char* request_uri(worker_t *w)
{
	return WI->request_uri;
}

static const char* request_querystring(worker_t *w)
{
	return WI->query_string;
}

static const char* request_contenttype(worker_t *w)
{
	return WI->content_type;
}

static const char* request_get_header(worker_t *w, const char *header, int *len)
{
	param_t *p = WI->req_header_param->find(WI->req_header_param, header);
	if (!p)
		return NULL;
	if (len)
		*len = p->value->offset;
	return p->value->data;
}

static const char* request_get_header_by_index(worker_t *w, int index, const char **value, int *vlen)
{
	param_t *p = WI->req_header_param->get(WI->req_header_param, index);
	if (!p)
		return NULL;
	if (value)
		*value = p->value->data;
	if (vlen)
		*vlen = p->value->offset;
	return p->key->data;
}

static const char* request_get_param(worker_t *w, const char *param_name, int *vlen)
{
	param_t *p = WI->get_param->find(WI->get_param, param_name);
	if (!p)
		return NULL;
	if (vlen)
		*vlen = p->value->offset;
	return p->value->data;
}

static const char* request_get_param_by_index(worker_t *w, int index, const char **value, int *vlen)
{
	param_t *p = WI->get_param->get(WI->get_param, index);
	if (!p)
		return NULL;
	if (value)
		*value = p->value->data;
	if (vlen)
		*vlen = p->value->offset;
	return p->key->data;
}

static const char* request_post_param(worker_t *w, const char *param_name, unsigned int *vlen)
{
	param_t *p = WI->post_param->find(WI->post_param, param_name);
	if (p) {
		if (vlen)
			*vlen = p->value->offset;
		return p->value->data;
	}
	form_param_t *fp = WI->form_param->find(WI->form_param, param_name);
	if (fp) {
		if (vlen)
			*vlen = fp->content->offset;
		return fp->content->data;
	}
	return NULL;
}

static const char* request_generic_post_param(worker_t *w, const char *param_name, unsigned int *vlen)
{
	param_t *p = WI->post_param->find(WI->post_param, param_name);
	if (p) {
		if (vlen)
			*vlen = p->value->offset;
		return p->value->data;
	}
	form_param_t *fp = WI->form_param->find_post(WI->form_param, param_name);
	if (fp) {
		if (vlen)
			*vlen = fp->content->offset;
		return fp->content->data;
	}
	return NULL;
}

static const char* request_file_post_param(worker_t *w, const char *param_name, unsigned int *vlen, const char **file_name)
{
	form_param_t *fp = WI->form_param->find_file(WI->form_param, param_name);
	if (fp) {
		if (vlen)
			*vlen = fp->content->offset;
		if (file_name)
			*file_name = fp->filename->data;
		return fp->content->data;
	}
	return NULL;
}

static const char* request_post_param_by_index(worker_t *w, int index, const char **value, unsigned int *vlen)
{
	param_t *p = WI->post_param->get(WI->post_param, index);
	if (p) {
		if (value)
			*value = p->value->data;
		if (vlen)
			*vlen = p->value->offset;
		return p->key->data;
	}
	form_param_t *fp = WI->form_param->get(WI->form_param, index);
	if (fp) {
		*value = fp->content->data;
		if (vlen)
			*vlen = fp->content->offset;
		return fp->name->data;
	}
	return NULL;
}

static const char* request_generic_post_param_by_index(worker_t *w, int index, const char **value, unsigned int *vlen)
{
	param_t *p = WI->post_param->get(WI->post_param, index);
	if (p) {
		if (value)
			*value = p->value->data;
		if (vlen)
			*vlen = p->value->offset;
		return p->key->data;
	}
	form_param_t *fp = WI->form_param->get_post(WI->form_param, index);
	if (fp) {
		*value = fp->content->data;
		if (vlen)
			*vlen = fp->content->offset;
		return fp->name->data;
	}
	return NULL;
}

static const char* request_file_post_param_by_index_b(worker_t *w, int index, buf_t **v, const char **file_name, const char **content_type)
{
	form_param_t *fp = WI->form_param->get_file(WI->form_param, index);
	if (fp) {
		*v = fp->content;
		if (file_name)
			*file_name = fp->filename->data;
		if (content_type)
			*content_type = fp->content_type->data;
		return fp->name->data;
	}
	return NULL;
}

static const char* request_file_post_param_by_index(worker_t *w, int index, const char **value, unsigned int *vlen, const char **file_name, const char **content_type)
{
	buf_t *v;
	const char* p = request_file_post_param_by_index_b(w, index, &v, file_name, content_type);
	if (p) {
		*value = v->data;
		if (vlen)
			*vlen = v->offset;
	}
	return p;
}

static const char* request_body(worker_t *w, unsigned int *blen, buf_t **body)
{
	if (blen)
		*blen = WI->req_body->offset;
	if (body)
		*body = WI->req_body;
	return WI->req_body->data;
}

static buf_t* request_dump(worker_t *w)
{
	buf_t *dump = w->pool->lend(w->pool, WI->req_body->offset + 128, 0);
	char **p = __envp(w);
	for (; p && *p; p++)
		buf_printf_ext(dump, "%s\r\n", *p);
	buf_printf_ext(dump, "\r\n");
	buf_append(dump, WI->req_body->data, WI->req_body->offset);
	return dump;
}

static int request_ismock(worker_t *w)
{
	return WI->req_ismock;
}

static void response_add_header(worker_t *w, const char *key, const char *value)
{
	/*
	 * We don't know the correct final value of 'Content-Length' before sending
	 * it (e.g. the gzip case etc). so, ignore it now.
	 */
	if (key && value && strcasecmp(key, "Content-Length")) {
		/* cover=0, which will allow multi-values be set (such cookie) */
		int cover = 0;
		WI->res_header_param->set(WI->res_header_param, key, value, 0, cover);
	}
}

static void response_clear_headers(worker_t *w)
{
	WI->res_header_param->reset(WI->res_header_param);
}

static int fcgi_send(const char *data, int len, worker_t *w)
{
	/*
	 * Response now but it will not affect the remaining work in handle.
	 * Note the following response actions will be discarded.
	 */
	if (WI->res_finished)
		return 0;
	int n = FCGX_PutStr(data, len, WI->req.out);
	FCGX_FFlush(WI->req.out);
	WI->res_finished = 1;
	if (++WI->res_times < 0)
		WI->res_times = 1;
	return n;
}

static int __send(worker_t *w, buf_t *content)
{
	if (!content || content->offset <= 0 || !content->data)
		return 0;
	return fcgi_send(content->data, content->offset, w);
}

static void fill_headers(param_t *p, buf_t *buf)
{
	if (p && p->key->offset > 0 && p->value->offset > 0)
		buf_printf_ext(buf, "%s: %s\r\n", p->key->data, p->value->data);
}

static int send_header(worker_t *w)
{
	buf_printf(WI->buf, "HTTP/1.1 200 OK\r\nStatus: 200 OK\r\n");
	WI->res_header_param->print(WI->res_header_param, &fill_headers, WI->buf);
	if (!WI->res_header_param->find(WI->res_header_param, "Content-Type"))
		buf_printf_ext(WI->buf, "Content-Type: text/html\r\n");
	buf_append(WI->buf, "\r\n", 2);
	return fcgi_send(WI->buf->data, WI->buf->offset, w);
}

static int redirect(worker_t *w, const char *location)
{
	if (!location)
		return 0;
	int len = strlen(location);
	if (len <= 0)
		return 0;
	/*
	 * Don't use buf_printf because the location maybe contains '%' such as '%2F' etc.
	 * buf_printf(WI->buf, "HTTP/1.1 302 Moved Temporarily\r\nLocation: %s\r\n\r\n", location);
	 */
	buf_printf(WI->buf, "HTTP/1.1 302 Moved Temporarily\r\nLocation: ");
	buf_append(WI->buf, location, len);
	buf_append(WI->buf, "\r\n\r\n", 4);
	return fcgi_send(WI->buf->data, WI->buf->offset, w);
}

static int __send_http(worker_t *w, int rescode, const char *desc, buf_t *content)
{
	buf_printf(WI->buf, "HTTP/1.1 %d %s\r\nStatus: %d %s\r\n", rescode, desc, rescode, desc);
	WI->res_header_param->print(WI->res_header_param, &fill_headers, WI->buf);
	if (!WI->res_header_param->find(WI->res_header_param, "Content-Type"))
		buf_printf_ext(WI->buf, "Content-Type: text/html\r\n");
	if (content && content->offset > 0) {
		buf_printf_ext(WI->buf, "Content-Length: %u\r\n\r\n", content->offset);
		buf_append(WI->buf, content->data, content->offset);
	} else
		buf_append(WI->buf, "\r\n", 2);
	return fcgi_send(WI->buf->data, WI->buf->offset, w);
}

static int send_http(worker_t *w, int rescode, buf_t *content)
{
	if (rescode < 100 || rescode > 511)
		return -1;
	return __send_http(w, rescode, check_rescode(rescode), content);
}

static int echo(worker_t *w, buf_t *content)
{
	if (!content || content->offset <= 0 || !content->data)
		return 0;
	return __send_http(w, 200, "OK", content);
}

static worker_t* worker_new(int index, litem_t *this)
{
	worker_t *w = (worker_t*)xcalloc(1, sizeof(worker_t));
	w->index = index;
	w->pool = buf_pool_new();
	w->priv_sys = worker_inner_new(w->pool, this);
	w->log = log_new(&__s->logbase, __s->log_buf_count, w->pool, 5120);

	w->launched = launched;
	w->once_over = once_over;
	w->log_record = log_record;
	w->launch_info = launch_info;
	w->fuse_check = fuse_check;

	w->request_headers_parse = request_headers_parse;
	w->request_get_parse = request_get_parse;
	w->request_post_parse = request_post_parse;
	w->fcgi_params = fcgi_params;
	w->get_fcgi_param = get_fcgi_param;
	w->request_accept = request_accept;
	w->request_method = request_method;
	w->request_uri = request_uri;
	w->request_querystring = request_querystring;
	w->request_contenttype = request_contenttype;
	w->request_get_header = request_get_header;
	w->request_get_header_by_index = request_get_header_by_index;
	w->request_get_param = request_get_param;
	w->request_get_param_by_index = request_get_param_by_index;
	w->request_post_param = request_post_param;
	w->request_generic_post_param = request_generic_post_param;
	w->request_file_post_param = request_file_post_param;
	w->request_post_param_by_index = request_post_param_by_index;
	w->request_generic_post_param_by_index = request_generic_post_param_by_index;
	w->request_file_post_param_by_index_b = request_file_post_param_by_index_b;
	w->request_file_post_param_by_index = request_file_post_param_by_index;
	w->request_body = request_body;
	w->request_dump = request_dump;
	w->request_ismock = request_ismock;

	w->response_add_header = response_add_header;
	w->response_clear_headers = response_clear_headers;
	w->send = __send;
	w->send_header = send_header;
	w->redirect = redirect;
	w->send_http = send_http;
	w->echo = echo;

	return w;
}

static void worker_free(void *data)
{
	worker_t *w = (worker_t*)data;
	if (w->priv_sys)
		worker_inner_free(WI);
	if (w->log)
		log_free(w->log);
	if (w->pool)
		buf_pool_free(w->pool);
	free(w);
}

static void worker_reset(worker_t *w)
{
	WI->req_header_param->reset(WI->req_header_param);
	WI->res_header_param->reset(WI->res_header_param);
	WI->get_param->reset(WI->get_param);
	WI->post_param->reset(WI->post_param);
	WI->form_param->reset(WI->form_param);
	if (WI->req_ismock) {
		WI->req_mock_param->clear(WI->req_mock_param);
		if (WI->req_mockenvp)
			free(WI->req_mockenvp);
		WI->req_mockenvp = NULL;
		WI->req_ismock = 0;
	}
	WI->res_finished = 0;
	WI->fuse = NULL;
	w->pool->reset(w->pool);
}

static void thread_key_build()
{
	pthread_key_create(&__thread_key, NULL);
}

static void* worker_run(void *args)
{
	worker_t *w = (worker_t*)args;
	pthread_once(&__thread_once, &thread_key_build);
	pthread_setspecific(__thread_key, w);

	sig_install_worker();
	if (__s->sopt->app_init(w) < 0) {
		BOOT_DONE(-1);
		kill(getpid(), SIGTERM);
		return NULL;
	}
	/*
	 * We put the routine in an infinite loop for avoiding the data
	 * and the stack information of thread be destroyed by GC.
	 */
	for (;;) { if (__s->sopt->app_handle(w) < 0) break; }

	app_free(w, 1);
	EXIT_COUNT();
	ERROR(w->log, "Core - Worker(%d) exited due to exception caused by reloading, logsync or anything else unknown", w->index);
	return NULL;
}

static void spirit_gc()
{
	time_t t = time(NULL);
	if (t - __spirit.gc_lasttime > GC_INTERVAL) {
		__s->sopt->gc();
		__spirit.gc_lasttime = t;
	}
}

static void report_url()
{
	spirit_t *p = &__spirit;
	p->fetcher->post(p->fetcher, p->url->data, p->buf->data, p->buf->offset, 0);
}

static void report_console()
{
	DEBUG(__spirit.log, "Core - Runtime=%s", __spirit.buf->data);
}

static void spirit_report(void (*f)())
{
	/*
	 * Issue 1
	 * thread_exited >= thread_count: It indicates the process has lost its working significance completely.
	 *
	 * Issue 2
	 * The reasons about mute_times > 0 maybe are:
	 * 1. Request was failed at request_basic_parse()
	 * 2. Application didn't response
	 * 3. It's being handled and busy now 
	 *
	 * Issue 3
	 * The statistic of error_times/fatal_times just about the logs generated in irmakit level only.
	 */
	long req_times, res_times, error_times, fatal_times;
	time_t req_lasttime, error_lasttime, fatal_lasttime;
	char rl[20], rql[20], el[20], fl[20];
	static char *lstr[] = { "debug", "event", "warn", "error", "fatal", "tc" };

	req_times = res_times = error_times = fatal_times = 0L;
	req_lasttime = error_lasttime = fatal_lasttime = 0L;

	litem_t *li = __workers->tail;
	do {
		worker_t *w = (worker_t*)li->data;
		req_times += WI->req_times;
		res_times += WI->res_times;
		error_times += WI->error_times;
		fatal_times += WI->fatal_times;
		if (WI->req_lasttime > req_lasttime)
			req_lasttime = WI->req_lasttime;
		if (WI->error_lasttime > error_lasttime)
			error_lasttime = WI->error_lasttime;
		if (WI->fatal_lasttime > fatal_lasttime)
			fatal_lasttime = WI->fatal_lasttime;

		li = li->link;
	} while (li != __workers->tail)
		;

	spirit_t *p = &__spirit;
	buf_printf(p->buf, "{"
		"\"host\":\"%s\", "
		"\"app\":\"%s\", "
		"\"ver\":\"%s\", "
		"\"pid\":\"%u\", "
		"\"thread_count\":%d, "
		"\"thread_exited\":%d, "
		"\"logbase\":\"%s\", "
		"\"start_time\":\"%s\", "
		"\"reload_times\":%ld, "
		"\"reload_lasttime\":\"%s\", "
		"\"req_times\":%ld, "
		"\"req_lasttime\":\"%s\", "
		"\"mute_times\":%ld, "
		"\"error_times\":%ld, "
		"\"error_lasttime\":\"%s\", "
		"\"fatal_times\":%ld, "
		"\"fatal_lasttime\":\"%s\""
		"}",
		p->host,
		p->app->data,
		p->ver->data,
		__pid,
		__s->thread_count,
		__thread_exited,
		lstr[__logbase_ori],
		p->start_time,
		__s->reload_times,
		time2string(&__s->reload_lasttime, rl),
		req_times,
		time2string(&req_lasttime, rql),
		req_times > res_times ? req_times-res_times : 0L,
		error_times,
		time2string(&error_lasttime, el),
		fatal_times,
		time2string(&fatal_lasttime, fl)
	);
	f();
}

static void spirit_pouroff_log()
{
	int day = today();
	if (day != __spirit.today) {
		EVENT(__spirit.log, "Core - Pour off logs because the date has changed");
		int ls = 2;
		__workers->apply(__workers, &worker_logsync, (void*)&ls);
		__spirit.today = day;
	}
}

static void spirit_logbase_check()
{
	if (__s->logbase == __logbase_ori)
		return;
	time_t t = time(NULL);
	if (t - __logbase_st < LOGBASE_ST_MAX)
		return;
	EVENT(__spirit.log, "Core - Auto-switch logbase to ORI due to the switch time has expired");
	__s->logbase = __logbase_ori;
	__logbase_st = t;
}

static void* spirit_run(void *args)
{
	sig_install_worker();
	for (;;) {
		if (!__main_reloading) {
			spirit_gc();
			if (__spirit.fetcher)
				REPORT_URL();
		}
		spirit_pouroff_log();
		spirit_logbase_check();
		thread_sleep(SPIRIT_INTERVAL);
	}
	return NULL;
}

static int service_check(service_t *s)
{
	if (!s || !s->sopt || !s->sopt->opt_parse || !s->sopt->app_init || !s->sopt->app_handle || !s->sopt->app_free)
		return -1;
	if (s->log_buf_count > LOGBUF_COUNT_MAX)
		s->log_buf_count = LOGBUF_COUNT_MAX;
	s->thread_count = 1;
	s->logbase = LT_EVENT;
	s->console_printf = console_printf;
	__s = s;
	return 0;
}

static int workers_boot()
{
	__workers = list_new(&worker_free);
	int i = 0;
	for (; i < __s->thread_count; i++) {
		litem_t *p = __workers->put(__workers, NULL, 0L);
		p->data = worker_new(i, p);
	}
	/* Boot the workers iteratively. */
	worker_t *w = __workers->tail->data;
	return pthread_create(&w->ppid, 0, &worker_run, (void*)w) == 0;
}

static int spirit_boot()
{
	/*
	 * Besides of reporting runtime data, spirit will also perform the GC and pours
	 * off old logs generated in the past day etc. so it will always be launched.
	 */
	spirit_t *p = &__spirit;
	memset(p, 0, sizeof(spirit_t));
	worker_t *w = (worker_t*)__workers->tail->link->data;
	p->bmax = WI->bmax;
	p->today = today();
	p->gc_lasttime = time(NULL);
	p->buf = g_buf_pool->lend(g_buf_pool, 0, 0);
	p->app = g_buf_pool->lend(g_buf_pool, 0, 0);
	p->ver = g_buf_pool->lend(g_buf_pool, 0, 0);
	p->url = g_buf_pool->lend(g_buf_pool, 0, 0);
	buf_copy(WI->app, p->app);
	buf_copy(WI->ver, p->ver);
	gethostname(p->host, sizeof(p->host));
	time2string(&__s->start_time, p->start_time);
	p->logbase = LT_DEBUG;
	p->log = log_new_simple(&p->logbase, 1024);
	if (WI->spirit_url->offset > 7) {
		buf_copy(WI->spirit_url, p->url);
		p->fetcher = fetcher_new(g_buf_pool);
	}
	return pthread_create(&p->ppid, 0, &spirit_run, NULL) == 0;
}

/*
 *---------------------------------------------------------------------------------
 * Service API
 *---------------------------------------------------------------------------------
 */
int service_run(int argc, char *argv[], service_t *s)
{
	if (service_check(s) < 0)
		return -1;
	__pid = PID;
	int common_free = 0, ret = -1;
	if (s->sopt->opt_parse(argc, argv, s, &g_log_build) < 0)
		goto __out_free;
	__logbase_ori = __s->logbase;
	/* We can't log anything until s->sopt->opt_parse() is invoked. */
	EVENT(g_log, "Core - irmacall version(%s)", s->irmacall_ver);
	if (s->sopt->common_init && s->sopt->common_init(s->thread_count, argc - 1, argv + 1) < 0) {
		FATAL(g_log, "Core - Fail to initialize the common module");
		goto __out_free;
	}
	common_free = 1;

	FCGX_Init();
	irmacurl_global_init();
	sig_install_main();
	BOOT_READY();
	if (!workers_boot()) {
		FATAL(g_log, "Core - Fail to boot workers because of the unknown reasons of system");
		goto __out_free;
	}
	BOOT_WAIT();
	if (__main_booting < 0) {
		FATAL(g_log, "Core - Workers' booting is not sufficient and please check whether the set of 'thread_count' is too large");
		goto __out_free;
	}
	ret = 0;
	time(&s->start_time);
	EVENT(g_log, "Core - Total (%d) workers have been booted up successfully", s->thread_count);
	if (!spirit_boot())
		WARN(g_log, "Core - Fail to boot spirit");

__out_free:
	if (__workers) {
		suspend();
		EVENT(g_log, "Core - Total (%d) workers have exited successfully", __thread_exited);
	}
	__free(common_free);
	return ret;
}

worker_t* worker_self()
{
	return pthread_getspecific(__thread_key);
}
