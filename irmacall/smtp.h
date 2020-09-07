#ifndef __SMTP_H__
#define __SMTP_H__

#include "buf.h"
#include "irmacurl.h"

#ifdef __cplusplus
extern "C" {
#endif

typedef struct __smtp smtp_t;
struct __smtp {
	void	*priv;
	buf_t	*to;
	buf_t	*subject;
	buf_t	*content;
	buf_t	*attachment[3];
	char	error[CURL_ERROR_SIZE];
	int		(*mail)(smtp_t *s, int hideto, int verbose);
	void	(*clean)(smtp_t *s);
};

smtp_t* smtp_new(buf_pool_t *pool, const char *server, const char *user, const char *password);
void smtp_free(void *s);

#ifdef __cplusplus
}
#endif

#endif
