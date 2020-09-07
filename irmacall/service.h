#ifndef __SERVICE_H__
#define __SERVICE_H__

#include <time.h>
#include <pthread.h>
#include "buf.h"
#include "param.h"
#include "log.h"

#ifdef __cplusplus
extern "C" {
#endif

typedef struct __runtime runtime_t;
typedef struct __worker worker_t;
typedef struct __sopt sopt_t;
typedef struct __service service_t;

struct __worker {
	pthread_t	ppid;
	int			index;
	buf_pool_t	*pool;
	log_t		*log;
	void		*priv_sys;
	void		*priv_app;

	void		(*launch_info)(worker_t *w, const char *app, const char *ver, long bmax, const char *url);
	void		(*launched)(worker_t *w);
	void		(*once_over)(worker_t *w);
	void		(*log_record)(worker_t *w, logtype_t type);

	int			(*fuse_check)(worker_t *w, const char *handler);
	int			(*request_ismock)(worker_t *w);
	int			(*request_accept)(worker_t *w);
	int			(*request_headers_parse)(worker_t *w);
	int			(*request_get_parse)(worker_t *w);
	int			(*request_post_parse)(worker_t *w, int *file_count);
	char**		(*fcgi_params)(worker_t *w);
	const char*	(*get_fcgi_param)(worker_t *w, const char *param_name);
	const char*	(*request_method)(worker_t *w);
	const char*	(*request_uri)(worker_t *w);
	const char*	(*request_querystring)(worker_t *w);
	const char*	(*request_contenttype)(worker_t *w);
	const char*	(*request_get_header)(worker_t *w, const char *header, int *len);
	const char*	(*request_get_header_by_index)(worker_t *w, int index, const char **value, int *vlen);
	const char*	(*request_get_param)(worker_t *w, const char *param_name, int *vlen);
	const char*	(*request_get_param_by_index)(worker_t *w, int index, const char **value, int *vlen);
	const char*	(*request_post_param)(worker_t *w, const char *param_name, unsigned int *vlen);
	const char*	(*request_generic_post_param)(worker_t *w, const char *param_name, unsigned int *vlen);
	const char*	(*request_file_post_param)(worker_t *w, const char *param_name, unsigned int *vlen, const char **file_name);
	const char*	(*request_post_param_by_index)(worker_t *w, int index, const char **value, unsigned int *vlen);
	const char*	(*request_generic_post_param_by_index)(worker_t *w, int index, const char **value, unsigned int *vlen);
	const char*	(*request_file_post_param_by_index_b)(worker_t *w, int index, buf_t **v, const char **file_name, const char **content_type);
	const char*	(*request_file_post_param_by_index)(worker_t *w, int index, const char **value, unsigned int *vlen, const char **file_name, const char **content_type);
	const char*	(*request_body)(worker_t *w, unsigned int *blen, buf_t **body);
	buf_t*		(*request_dump)(worker_t *w);

	void		(*response_add_header)(worker_t *w, const char *key, const char *value);
	void		(*response_clear_headers)(worker_t *w);
	int			(*send)(worker_t *w, buf_t *content);
	int			(*send_header)(worker_t *w);
	int			(*redirect)(worker_t *w, const char *location);
	int			(*send_http)(worker_t *w, int rescode, buf_t *content);
	int			(*echo)(worker_t *w, buf_t *content);
};

struct __sopt {
	int	(*opt_parse)(int argc, char *argv[], service_t *s, int (*g_log_build)());
	int	(*common_init)(int thread_count, int argc, char *argv[]);
	int	(*app_init)(worker_t *w);
	int	(*app_handle)(worker_t *w);
	int	(*common_reload)(int thread_count);
	int	(*app_reload)(worker_t *w);
	int	(*app_is_busy)(worker_t *w);
	int	(*app_free)(worker_t *w, int finalize);
	int	(*common_free)(int thread_count);
	int	(*gc)();
};

struct __service {
	const char	*irmacall_ver;
	const char	*log_prefix;
	int			log_buf_count;
	int			thread_count;
	int			mock_support;
	logtype_t	logbase;
	time_t		start_time;
	time_t		reload_lasttime;
	long		reload_times;
	int			(*usage)(service_t *s);
	int			(*version)(service_t *s);
	int			(*console_printf)(const char *fmt, ...);
	sopt_t		*sopt;
};

extern log_t *g_log;
extern buf_pool_t *g_buf_pool;

/* API */
#define CURRENT	worker_self()
int service_run(int argc, char *argv[], service_t *s);
worker_t* worker_self();

#ifdef __cplusplus
}
#endif

#endif
