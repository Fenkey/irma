#ifndef __WRAPPER_H__
#define __WRAPPER_H__

#include <stdlib.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef void (*die_routine_t)();
die_routine_t set_die_routine(die_routine_t dr);

void* xmalloc(size_t size);
void* xcalloc(size_t nmemb, size_t size);
void* xrealloc(void *ptr, size_t size);
char* xstrdup(const char *s);

#ifdef __cplusplus
}
#endif

#endif
