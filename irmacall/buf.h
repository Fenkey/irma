#ifndef __BUF_H__
#define __BUF_H__

#ifdef __cplusplus
extern "C" {
#endif

#include <stdio.h>

typedef struct __buf_pool buf_pool_t;
typedef struct __buf buf_t;
struct __buf {
	char			*data;
	unsigned int	size;
	unsigned int	offset;
	void			*priv;
};

buf_t*	buf_new();
int		buf_is_lent(buf_t *buf);
int		buf_is_auto(buf_t *buf);
char*	buf_fgets(buf_t *buf, FILE *f);
char*	buf_data(buf_t *buf, unsigned int len);
char*	buf_printf(buf_t *buf, const char *fmt, ...);
char*	buf_printf_ext(buf_t *buf, const char *fmt, ...);
char*	buf_trim(buf_t *buf);
void	buf_append(buf_t *buf, const char *data, int len);
void	buf_insert(buf_t *buf, char *where, const char *data, int len);
void	buf_copy(const buf_t *from, buf_t *to);
void	buf_reset(buf_t *buf);
void	buf_force_reset(buf_t *buf);
void	buf_data_reset(buf_t *buf, unsigned int len);
void	buf_return(buf_t *buf);
void	buf_free(void *data);

struct __buf_pool {
	void			*priv;
	buf_t*			(*lend)(buf_pool_t *pool, unsigned int size, int auto_reset);
	void			(*reset)(buf_pool_t *pool);
	void			(*dry)(buf_pool_t *pool);
	int				(*busy_count)(buf_pool_t *pool);
	int				(*free_count)(buf_pool_t *pool);
	int				(*count)(buf_pool_t *pool);
	long long		(*busy_sum)(buf_pool_t *pool);
	long long		(*free_sum)(buf_pool_t *pool);
	long long		(*sum)(buf_pool_t *pool);
	unsigned int	(*busy_max)(buf_pool_t *pool);
	unsigned int	(*free_max)(buf_pool_t *pool);
	unsigned int	(*max)(buf_pool_t *pool);
};

buf_pool_t*	buf_pool_new();
void buf_pool_free(buf_pool_t *pool);

#ifdef __cplusplus
}
#endif

#endif
