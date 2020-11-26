#include <assert.h>
#include "wrapper.h"
#include "list.h"

static litem_t* __put(list_t *l, litem_t *p, int prepend)
{
	litem_t *newtail = p;
	if (l->tail) {
		p->link = l->tail->link;
		l->tail->link = p;
		if (prepend)
			newtail = l->tail;
	} else
		p->link = p;
	l->tail = newtail;
	return p;
}

static litem_t* put(list_t *l, void *data, time_t timestamp)
{
	assert(l);
	litem_t *p = (litem_t*)xcalloc(1, sizeof(litem_t));
	p->data = data;
	p->timestamp = timestamp;
	return __put(l, p, 0);
}

static void* get(list_t *l, time_t *timestamp)
{
	assert(l);
	if (!l->tail)
		return NULL;
	litem_t *p = l->tail->link;
	void *data = p->data;
	if (timestamp)
		*timestamp = p->timestamp;
	if (p == l->tail)
		l->tail = NULL;
	else
		l->tail->link = p->link;
	free(p);
	return data;
}

static litem_t* index(list_t *l, int i)
{
	assert(l && i >= 0);
	if (!l->tail)
		return NULL;
	litem_t *h = l->tail->link, *p = h;
	do {
		if (i-- == 0)
			return p;
		p = p->link;
	} while (p != h)
		;
	return NULL;
}

static litem_t* first(list_t *l)
{
	assert(l);
	return l->tail ? l->tail->link : NULL;
}

static litem_t* last(list_t *l)
{
	assert(l);
	return l->tail;
}

static litem_t* find(list_t *l, list_dfind_t dfind, void *val)
{
	assert(l && dfind);
	if (!l->tail)
		return NULL;
	litem_t *p = l->tail;
	do {
		if (dfind(p->data, val))
			return p;
		p = p->link;
	} while (p != l->tail)
		;
	return NULL;
}

static litem_t** findall(list_t *l, list_dfind_t dfind, void *val, int *count)
{
	assert(l && dfind && count);
	*count = 0;
	if (!l->tail)
		return NULL;
	litem_t *p = l->tail;
	do {
		if (dfind(p->data, val))
			(*count)++;
		p = p->link;
	} while (p != l->tail)
		;
	if (!*count)
		return NULL;
	int i = 0;
	litem_t **pp = (litem_t**)xcalloc(*count, sizeof(litem_t*));
	do {
		if (dfind(p->data, val))
			pp[i++] = p;
		p = p->link;
	} while (i < *count && p != l->tail)
		;
	return pp;
}

static int apply(list_t *l, list_dapply_t dapply, void *val)
{
	assert(l && dapply);
	if (!l->tail)
		return 0;
	int ok = 0;
	litem_t *h = l->tail->link, *p = h;
	do {
		if (dapply(p->data, val))
			ok++;
		p = p->link;
	} while (p != h)
		;
	return ok;
}

static int mv(litem_t *p, list_t *from, list_t *to, int prepend)
{
	assert(p && from && to);
	if (!from->tail)
		return 0;
	if (from->tail == p && (from->tail = p->link) == p)
		from->tail = NULL;
	else {
		litem_t *tmp = p;
		while (tmp->link != p)
			tmp = tmp->link;
		tmp->link = p->link;
	}
	__put(to, p, prepend);
	return 1;
}

static void* remove(list_t *l, litem_t *p)
{
	assert(l && p);
	if (!l->tail)
		return NULL;
	if (l->tail == p && (l->tail = p->link) == p)
		l->tail = NULL;
	else {
		litem_t *tmp = p;
		while (tmp->link != p)
			tmp = tmp->link;
		tmp->link = p->link;
	}
	void *data = p->data;
	free(p);
	return data;
}

static int del(list_t *l, litem_t *p)
{
	assert(l && p);
	if (!l->tail)
		return 0;
	if (l->tail == p && (l->tail = p->link) == p)
		l->tail = NULL;
	else {
		litem_t *tmp = p;
		while (tmp->link != p)
			tmp = tmp->link;
		tmp->link = p->link;
	}
	if (p->data && l->dfree)
		l->dfree(p->data);
	free(p);
	return 1;

}

static int append(list_t *to, list_t *from)
{
	assert(to && from);
	if (!from->tail || to->dfree != from->dfree)
		return 0;
	litem_t *p = from->tail;
	do {
		put(to, p->data, p->timestamp);
		p = p->link;
	} while (p != from->tail)
		;
	return 1;
}

static int count(list_t *l)
{
	assert(l);
	if (!l->tail)
		return 0;
	int n = 1;
	litem_t *p = l->tail;
	for (; p->link != l->tail; p = p->link, n++)
		;
	return n;
}

static int isempty(list_t *l)
{
	assert(l);
	return l->tail != NULL;
}

static int isfirst(list_t *l, litem_t *li)
{
	assert(l && li);
	return l->tail && li == l->tail->link;
}

static int islast(list_t *l, litem_t *li)
{
	assert(l && li);
	return l->tail && li == l->tail;
}

static void __clear(list_t *l)
{
	litem_t *p = l->tail;
	do {
		litem_t *tmp = p->link;
		if (p->data && l->dfree)
			l->dfree(p->data);
		free(p);
		p = tmp;
	} while (p != l->tail)
		;
	l->tail = NULL;
}

static int clear(list_t *l)
{
	assert(l);
	if (!l->tail)
		return -1;
	__clear(l);
	return 0;
}

static void** toarray(list_t *l, int *len)
{
	assert(l);
	int n = count(l);
	if (n <= 0)
		return NULL;
	int i = 0;
	void **array = (void**)xcalloc(n, sizeof(void*));
	litem_t *h = l->tail->link, *p = h;
	do {
		array[i++] = p->data;
		p = p->link;
	} while (p != h)
		;
	if (len)
		*len = n;
	return array;
}

list_t* list_new(list_dfree_t dfree)
{
	list_t *l = (list_t*)xcalloc(1, sizeof(list_t));
	l->dfree = dfree;
	l->put = put;
	l->get = get;
	l->index = index;
	l->first = first;
	l->last = last;
	l->find = find;
	l->findall = findall;
	l->apply = apply;
	l->mv = mv;
	l->del = del;
	l->remove = remove;
	l->append = append;
	l->count = count;
	l->isempty = isempty;
	l->isfirst = isfirst;
	l->islast = islast;
	l->clear = clear;
	l->toarray = toarray;
	return l;
}

void list_free(void *val)
{
	assert(val);
	list_t *l = (list_t*)val;
	if (l->tail)
		__clear(l);
	free(l);
}
