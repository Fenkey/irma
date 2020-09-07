#ifndef __FETCHER_H__
#define __FETCHER_H__

#include "buf.h"
#include "irmacurl.h"

#ifdef __cplusplus
extern "C" {
#endif

typedef enum { GET = 0, POST, PUT, HEAD, DELETE } method_t;

typedef struct __fetcher fetcher_t;
struct __fetcher {
	void		*priv;
	buf_t		*req_url;
	method_t	req_method;
	buf_t		*res_headers;
	buf_t		*res_cookies;
	buf_t		*res_body;
	long		res_code;
	char		*res_content_type;
	char		error[CURL_ERROR_SIZE];
	double		time_used;
	
	int			(*append_header)(fetcher_t *f, const char *header);
	int			(*clear_headers)(fetcher_t *f);
	int			(*append_formpost_kv)(fetcher_t *f, const char *key, const char *value);
	int			(*append_formpost_file)(fetcher_t *f, const char *name, const char *file, const char *content_type);
	int			(*append_formpost_filebuf)(fetcher_t *f, const char *name, const char *file, const char *body, long len, const char *content_type);
	int			(*clear_formpost)(fetcher_t *f);
	long		(*get)(fetcher_t *f, const char *url, int timeout);
	long		(*post)(fetcher_t *f, const char *url, const char *body, int len, int timeout);
	long		(*postform)(fetcher_t *f, const char *url, int timeout);
	long		(*put)(fetcher_t *f, const char *url, const char *body, int len, int timeout);
	long		(*delete)(fetcher_t *f, const char *url, int timeout);
};

fetcher_t* fetcher_new(buf_pool_t *pool);
void fetcher_free(void *f);

#ifdef __cplusplus
}
#endif

#endif
