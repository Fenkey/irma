#ifndef __PARAM_H__
#define __PARAM_H__

#include "buf.h"

#ifdef __cplusplus
extern "C" {
#endif

typedef struct __param {
	buf_t	*key;
	buf_t	*value;
} param_t;

typedef void (*param_print_t)(param_t *param, buf_t *buf);

typedef struct __param_list paramlist_t;
struct __param_list {
	void		*priv;
	void		(*reset)(paramlist_t *plist);
	void		(*print)(paramlist_t *plist, param_print_t pf, buf_t *buf);
	int			(*count)(paramlist_t *plist);
	int			(*del)(paramlist_t *plist, param_t *param);
	param_t*	(*set)(paramlist_t *plist, const char *key, const char *value, int vlen, int cover);
	param_t*	(*get)(paramlist_t *plist, int i);
	param_t*	(*find)(paramlist_t *plist, const char *key);
	param_t**	(*findall)(paramlist_t *plist, const char *key, int *count);
	param_t*	(*ext)(paramlist_t *plist);
};

typedef void (*parse_cb_t)(param_t *param, buf_t *buf);

typedef struct __paramparser paramparser_t;
struct __paramparser {
	void			*priv;
	int				(*parse)(paramlist_t *plist, const char *str, int len, char sep1, char sep2, parse_cb_t cb);
	paramlist_t*	(*paramlist_new)(paramparser_t *parser);
	void			(*paramlist_free)(paramlist_t *plist);
};

paramparser_t* paramparser_new(buf_pool_t *pool);
void paramparser_free(paramparser_t *parser, int release_pool);

#ifdef __cplusplus
}
#endif

#endif
