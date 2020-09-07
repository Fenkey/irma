#ifndef __FORM_PARAM_H__
#define __FORM_PARAM_H__

#include "buf.h"

#ifdef __cplusplus
extern "C" {
#endif

typedef struct __form_param {
	buf_t	*name;
	buf_t	*filename;
	buf_t	*content_type;
	buf_t	*content;
} form_param_t;

typedef struct __form_param_list fparamlist_t;
struct __form_param_list {
	void			*priv;
	void			(*reset)(fparamlist_t *fplist);
	int				(*count)(fparamlist_t *fplist);
	int				(*post_count)(fparamlist_t *fplist);
	int				(*file_count)(fparamlist_t *fplist);
	form_param_t*	(*get)(fparamlist_t *fplist, int i);
	form_param_t*	(*get_post)(fparamlist_t *fplist, int i);
	form_param_t*	(*get_file)(fparamlist_t *fplist, int i);
	form_param_t*	(*find)(fparamlist_t *fplist, const char *key);
	form_param_t*	(*find_post)(fparamlist_t *fplist, const char *key);
	form_param_t*	(*find_file)(fparamlist_t *fplist, const char *key);
	form_param_t*	(*ext)(fparamlist_t *fplist);
};

typedef struct __form_param_parser fparamparser_t;
struct __form_param_parser {
	void			*priv;
	int				(*parse)(fparamlist_t *fplist, buf_t *body, const char *boundary, int blen, int *fcount);
	fparamlist_t*	(*fparamlist_new)(fparamparser_t *parser);
	void			(*fparamlist_free)(fparamlist_t *fplist);
};

fparamparser_t* fparamparser_new(buf_pool_t *pool);
void fparamparser_free(fparamparser_t *parser, int release_pool);

#ifdef __cplusplus
}
#endif

#endif
