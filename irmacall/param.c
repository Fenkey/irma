#include <ctype.h>
#include <string.h>
#include <strings.h>
#include <assert.h>
#include "wrapper.h"
#include "list.h"
#include "param.h"

#define PLI ((paramlist_inner_t*)plist->priv)
#define PPI ((paramparser_inner_t*)parser->priv)

typedef struct {
	int local;
	buf_pool_t *pool;
} paramparser_inner_t;

typedef struct {
	buf_t *buf;
	int count;
	list_t *list;
	paramparser_inner_t *parser;
} paramlist_inner_t;

struct pfpack {
	param_print_t pf;
	buf_t *buf;
};

static param_t* param_new(buf_pool_t *pool)
{
	param_t *p = xcalloc(1, sizeof(param_t));
	p->key = pool->lend(pool, 0, 0);
	p->value = pool->lend(pool, 0, 0);
	return p;
}

static void param_free(void *data)
{
	param_t *p = (param_t*)data;
	buf_return(p->key);
	buf_return(p->value);
	free(p);
}

static int param_print(void *data, void *val)
{
	param_t *p = (param_t*)data;
	struct pfpack *pack = (struct pfpack*)val;
	pack->pf(p, pack->buf);
	return 1;
}

static int param_find(void *data, void *val)
{
	param_t *p = (param_t*)data;
	return !strcasecmp(p->key->data, (char*)val);
}

/*
 * methods of list
 */
static void paramlist_reset(paramlist_t *plist)
{
	assert(plist);
	PLI->count = 0;
	buf_reset(PLI->buf);
	PLI->list->clear(PLI->list);
}

static void paramlist_print(paramlist_t *plist, param_print_t pf, buf_t *buf)
{
	assert(plist && pf);
	struct pfpack pack = { pf, buf };
	PLI->list->apply(PLI->list, &param_print, &pack);
}

static int paramlist_count(paramlist_t *plist)
{
	assert(plist);
	return PLI->count;
}

static int paramlist_del(paramlist_t *plist, param_t *param)
{
	assert(plist);
	if (!param)
		return 0;
	litem_t *li = PLI->list->find(PLI->list, &param_find, (void*)param->key->data);
	if (li && PLI->list->del(PLI->list, li)) {
		PLI->count--;
		return 1;
	}
	return 0;
}

static param_t* paramlist_set(paramlist_t *plist, const char *key, const char *value, int vlen, int cover)
{
	assert(plist);
	if (!key || !value)
		return NULL;
	param_t *p;
	if (cover) {
		litem_t *li = PLI->list->find(PLI->list, &param_find, (void*)key);
		if (!li) {
			/* count will be increased in ext(). */
			p = plist->ext(plist);
		} else
			p = li->data;
	} else
		p = plist->ext(plist);
	if (p->key->offset <= 0)
		buf_printf(p->key, "%s", key);
	buf_reset(p->value);
	buf_append(p->value, value, vlen <= 0 ? strlen(value) : vlen);
	return p;
}

static param_t* paramlist_get(paramlist_t *plist, int i)
{
	assert(plist);
	if (i < 0 || i >= PLI->count)
		return NULL;
	litem_t *li = PLI->list->index(PLI->list, i);
	return li ? li->data : NULL;
}

static param_t* paramlist_find(paramlist_t *plist, const char *key)
{
	assert(plist);
	if (!key)
		return NULL;
	litem_t *li = PLI->list->find(PLI->list, &param_find, (void*)key);
	return li ? li->data : NULL;
}

static param_t** paramlist_findall(paramlist_t *plist, const char *key, int *count)
{
	assert(plist);
	*count = 0;
	if (!key)
		return NULL;
	litem_t **ll = PLI->list->findall(PLI->list, &param_find, (void*)key, count);
	if (!*count)
		return NULL;
	param_t **pp = (param_t**)xcalloc(*count, sizeof(param_t*));
	int i = 0;
	for (; i < *count; i++)
		pp[i] = ll[i]->data;
	free(ll);
	return pp;
}

static param_t* paramlist_ext(paramlist_t *plist)
{
	assert(plist);
	param_t *p = param_new(PLI->parser->pool);
	PLI->list->put(PLI->list, p, 0L);
	PLI->count++;
	return p;
}

/*
 * methods of parser
 */
static paramlist_t* paramlist_new(paramparser_t *parser)
{
	assert(parser);
	paramlist_t *plist = xcalloc(1, sizeof(paramlist_t));
	plist->priv = xcalloc(1, sizeof(paramlist_inner_t));
	PLI->buf = PPI->pool->lend(PPI->pool, 0, 0);
	PLI->list = list_new(&param_free);
	PLI->parser = parser->priv;
	plist->reset = &paramlist_reset;
	plist->print = &paramlist_print;
	plist->count = &paramlist_count;
	plist->del = &paramlist_del;
	plist->set = &paramlist_set;
	plist->get = &paramlist_get;
	plist->find = &paramlist_find;
	plist->findall = &paramlist_findall;
	plist->ext = &paramlist_ext;
	return plist;
}

static void paramlist_free(paramlist_t *plist)
{
	assert(plist);
	buf_return(PLI->buf);
	list_free(PLI->list);
	free(plist->priv);
	free(plist);
}

static void split_2(paramlist_t *plist, const char *str, int len, int *count, char sep2, parse_cb_t cb)
{
	if (len <= 0 || !str || !*str)
		return;

	char *p = memchr(str, (int)sep2, len);
	if (p == str)
		return;
	param_t *new = plist->ext(plist);
	if (!p)
		buf_append(new->key, str, len);
	else {
		buf_append(new->key, str, p - str);
		if ((len -= p + 1 - str) > 0)
			buf_append(new->value, p + 1, len);
		else
			buf_append(new->value, " ", 1);
	}
	if (cb)
		cb(new, PLI->buf);
	*count += 1;
}

static void split_1(paramlist_t *plist, const char *str, int len, int *count, char sep1, char sep2, parse_cb_t cb)
{
	if (len <= 0 || !str || !*str)
		return;

	char *p = memchr(str, (int)sep1, len);
	if (!p)
		split_2(plist, str, len, count, sep2, cb);
	else if (p > str) {
		split_2(plist, str, p - str, count, sep2, cb);
		split_1(plist, p + 1, len - (p + 1 - str), count, sep1, sep2, cb);
	}
}

static int parse(paramlist_t *plist, const char *str, int len, char sep1, char sep2, parse_cb_t cb)
{
	assert(plist);
	plist->reset(plist);

	int count = 0;
	if (str) {
		if (len <= 0)
			len = strlen(str);
		split_1(plist, str, len, &count, sep1, sep2, cb);
		if (cb)
			buf_force_reset(PLI->buf);
	}
	PLI->count = count;
	return count;
}

paramparser_t* paramparser_new(buf_pool_t *pool)
{
	paramparser_t *parser = xmalloc(sizeof(paramparser_t));
	parser->priv = xcalloc(1, sizeof(paramparser_inner_t));
	PPI->local = !pool;
	PPI->pool = pool ? pool : buf_pool_new();
	parser->parse = &parse;
	parser->paramlist_new = &paramlist_new;
	parser->paramlist_free = &paramlist_free;
	return parser;
}

void paramparser_free(paramparser_t *parser, int release_pool)
{
	assert(parser && PPI->pool);
	if (PPI->local || release_pool)
		buf_pool_free(PPI->pool);
	free(parser->priv);
	free(parser);
}
