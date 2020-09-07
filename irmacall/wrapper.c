#include <stdarg.h>
#include <string.h>
#include <assert.h>
#include "wrapper.h"

static void routine_buildin() {}
static die_routine_t __dr = routine_buildin;

static void die(const char *err)
{
	exit(1);
}

die_routine_t set_die_routine(die_routine_t dr)
{
	die_routine_t old = __dr;
	__dr = dr;
	return old;
}

void* xmalloc(size_t size)
{
	void *p = malloc(size);
	if (!p) {
		if (__dr)
			__dr();
		die("Out of memory, fail to malloc.");
	}
	return p;
}

void* xcalloc(size_t nmemb, size_t size)
{
	void *p = calloc(nmemb, size);
	if (!p) {
		if (__dr)
			__dr();
		die("Out of memory, fail to calloc.");
	}
	return p;
}

void* xrealloc(void *ptr, size_t size)
{
	void *p = realloc(ptr, size);
	if (!p) {
		if (__dr)
			__dr();
		die("Out of memory, fail to realloc.");
	}
	return p;
}

char* xstrdup(const char *s)
{
	char *p = strdup(s);
	if (!p) {
		if (__dr)
			__dr();
		die("Out of memory, fail to strdup.");
	}
	return p;
}

