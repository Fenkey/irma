#include <string.h>
#include "wrapper.h"
#include "map.h"

#define MAPH0	7
#define MAPH1	9
#define MAPH2	13

typedef struct __mapobj mobj_t;
struct __mapobj {
	char *key;
	void *val;
	unsigned int sum;
	mobj_t *prev;
	mobj_t *next;
	mobj_t *hash_prev;
	mobj_t *hash_next;
	map_vfree_t vfree;
};

typedef struct {
	mobj_t *root;
	mobj_t *objs[MAPH0][MAPH1][MAPH2];
	unsigned int (*hv)(const char *key, int *i, int *j, int *k);
} map_inner_t;

static mobj_t* mobj_new(const char *key, void *val, unsigned int sum, map_vfree_t vfree)
{
	mobj_t *o = (mobj_t*)xcalloc(1, sizeof(mobj_t));
	o->key = strdup(key);
	o->val = val;
	o->sum = sum;
	o->vfree = vfree;
	return o;
}

static void* mobj_free(mobj_t *o)
{
	free(o->key);
	void *val = o->val;
	if (val && o->vfree) {
		o->vfree(val);
		val = NULL;
	}
	free(o);
	return val;
}

static unsigned int hv(const char *key, int *i, int *j, int *k)
{
	unsigned int sum = 0;
	unsigned char *p = (unsigned char*)key;
	do { sum += *p; } while (*p++);
	*i = sum % MAPH0;
	*j = sum % MAPH1;
	*k = sum % MAPH2;
	return sum;
}

static mobj_t* __get(mobj_t *hash_root, const char *key, unsigned int sum)
{
	mobj_t *o = hash_root;
	for (; o; o = o->hash_next) {
		if (o->sum == sum && !strcmp(o->key, key))
			return o;
	}
	return NULL;
}

static int len(map_t *m)
{
	map_inner_t *mi = (map_inner_t*)m->data;
	int n = 0;
	mobj_t *o = mi->root;
	for (; o; o = o->next, ++n)
		;
	return n;
}

static int set(map_t *m, const char *key, void *val, map_vfree_t vfree)
{
	map_inner_t *mi = (map_inner_t*)m->data;
	int i, j, k;
	unsigned int sum = mi->hv(key, &i, &j, &k);
	mobj_t *o = __get(mi->objs[i][j][k], key, sum);
	if (!o) {
		o = mobj_new(key, val, sum, vfree);
		o->next = mi->root;
		if (mi->root)
			mi->root->prev = o;
		mi->root = o;

		o->hash_next = mi->objs[i][j][k];
		if (mi->objs[i][j][k])
			mi->objs[i][j][k]->hash_prev = o;
		mi->objs[i][j][k] = o;
	} else {
		if (val && o->vfree)
			o->vfree(val);
		o->val = val;
		o->vfree = vfree;
	}
	return 1;
}

static void* get(map_t *m, const char *key)
{
	map_inner_t *mi = (map_inner_t*)m->data;
	int i, j, k;
	unsigned int sum = mi->hv(key, &i, &j, &k);
	mobj_t *o = __get(mi->objs[i][j][k], key, sum);
	return o ? o->val : NULL;
}

static void* del(map_t *m, const char *key)
{
	map_inner_t *mi = (map_inner_t*)m->data;
	int i, j, k;
	unsigned int sum = mi->hv(key, &i, &j, &k);
	mobj_t *o = __get(mi->objs[i][j][k], key, sum);
	if (!o)
		return NULL;
	if (o->prev)
		o->prev->next = o->next;
	if (o->next)
		o->next->prev = o->prev;
	if (o == mi->root)
		mi->root = o->next;

	if (o->hash_prev)
		o->hash_prev->hash_next = o->hash_next;
	if (o->hash_next)
		o->hash_next->hash_prev = o->hash_prev;
	if (o == mi->objs[i][j][k])
		mi->objs[i][j][k] = o->hash_next;

	return mobj_free(o);
}

map_t* map_new()
{
	map_t *m = (map_t*)xcalloc(1, sizeof(map_t));
	map_inner_t *mi = (map_inner_t*)xcalloc(1, sizeof(map_inner_t));
	mi->hv = hv;
	m->data = mi;
	m->len = len;
	m->set = set;
	m->get = get;
	m->del = del;
	return m;
}

void map_free(void *val)
{
	map_t *m = (map_t*)val;
	map_inner_t *mi = (map_inner_t*)m->data;
	mobj_t *o = mi->root;
	while (o) {
		mobj_t *tmp = o->next;
		mobj_free(o);
		o = tmp;
	}
	free(mi);
	free(m);
}
