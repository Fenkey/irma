#ifndef __IRMACURL_H__
#define __IRMACURL_H__

#include <pthread.h>
#include <openssl/crypto.h>
#include <curl/curl.h>

#ifdef __cplusplus
extern "C" {
#endif

#define DNS_CACHE_TIMEOUT 86400L

void irmacurl_global_init();
void irmacurl_global_free();

extern void *g_shareobj;

#ifdef __cplusplus
}
#endif

#endif
