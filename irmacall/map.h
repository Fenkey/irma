#ifndef __MAP_H__
#define __MAP_H__

#ifdef __cplusplus
extern "C" {
#endif

typedef void (*map_vfree_t)(void *val);

typedef struct __map map_t; 
struct __map {
	void	*data;
	int		(*len)(map_t *m);
	int		(*set)(map_t *m, const char *key, void *val, map_vfree_t vfree);
	void*	(*get)(map_t *m, const char *key);
	void*	(*del)(map_t *m, const char *key);
};

map_t* map_new();
void map_free(void *m);

#ifdef __cplusplus
}
#endif

#endif
