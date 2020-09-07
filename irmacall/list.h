#ifndef __LIST_H__
#define __LIST_H__

#ifdef __cplusplus
extern "C" {
#endif

#include <time.h>

typedef void (*list_dfree_t)(void *data);
typedef int (*list_dfind_t)(void *data, void *val);
typedef int (*list_dapply_t)(void *data, void *val);

typedef struct __list_item litem_t; 
struct __list_item {
	void		*data;
	time_t		timestamp;
	litem_t		*link;
};

typedef struct __list list_t;
struct __list {
	litem_t			*tail;
	list_dfree_t	dfree;

	litem_t*	(*put)(list_t *l, void *data, time_t timestamp);
	void*		(*get)(list_t *l, time_t *timestamp);
	litem_t*	(*index)(list_t *l, int i);
	litem_t*	(*find)(list_t *l, list_dfind_t dfind, void *val);
	litem_t**	(*findall)(list_t *l, list_dfind_t dfind, void *val, int *count);
	int			(*apply)(list_t *l, list_dapply_t dapply, void *val);
	int			(*mv)(litem_t *li, list_t *from, list_t *to, int prepend);
	int			(*del)(list_t *l, litem_t *li);
	void*		(*remove)(list_t *l, litem_t *li);
	int			(*append)(list_t *to, list_t *from);
	int			(*count)(list_t *l);
	int			(*isempty)(list_t *l);
	int			(*clear)(list_t *l);
	void**		(*toarray)(list_t *l, int *len);
};

list_t* list_new(list_dfree_t dfree);
void list_free(void *l);

#ifdef __cplusplus
}
#endif

#endif
