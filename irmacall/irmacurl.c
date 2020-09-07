#include "irmacurl.h"

void *g_shareobj = NULL;
static pthread_mutex_t *__lockarray = NULL;
static pthread_mutex_t __share_mutex = PTHREAD_MUTEX_INITIALIZER;

static void share_lock_cb(CURL *handle, curl_lock_data data, curl_lock_access access, void *userptr)
{
	pthread_mutex_lock(&__share_mutex);
}

static void share_unlock_cb(CURL *handle, curl_lock_data data, void *userptr)
{
	pthread_mutex_unlock(&__share_mutex);
}

static void ssl_lock_cb(int mode, int type, char *file, int line)
{
	if (mode & CRYPTO_LOCK)
		pthread_mutex_lock(&__lockarray[type]);
	else
		pthread_mutex_unlock(&__lockarray[type]);
}

static unsigned long thread_id()
{
	return pthread_self();
}

void irmacurl_global_init()
{
	curl_global_init(CURL_GLOBAL_ALL);

	/*
	 * The openssl isn't threads-safe and sometimes it might cause exceptions. We follow
	 * the official advice of openssl to set the lock:
	 *
	 * CRYPTO_set_id_callback((unsigned long (*)())thread_id);
	 * CRYPTO_set_locking_callback((void (*)())ssl_lock_cb);
	 * refer to docs/examples/threaded-ssl.c of libcurl
	 */
	__lockarray = (pthread_mutex_t*)OPENSSL_malloc(CRYPTO_num_locks() * sizeof(pthread_mutex_t));
	int i = 0;
	for (; i < CRYPTO_num_locks(); i++)
		pthread_mutex_init(&__lockarray[i], NULL);
	CRYPTO_set_id_callback((unsigned long (*)())thread_id);
	CRYPTO_set_locking_callback((void (*)())ssl_lock_cb);

	if ((g_shareobj = curl_share_init()) != NULL) {
		/*
		 * "If the curl handles are used simultaneously in multiple threads,
		 * you MUST use the locking methods in the share handle."
		 * refer to -
		 * https://curl.haxx.se/libcurl/c/CURLOPT_SHARE.html
		 * https://curl.haxx.se/libcurl/c/curl_share_setopt.html
		 * docs/examples/threaded-shared-conn.c
		 */
		curl_share_setopt((CURLSH*)g_shareobj, CURLSHOPT_LOCKFUNC, &share_lock_cb);
		curl_share_setopt((CURLSH*)g_shareobj, CURLSHOPT_UNLOCKFUNC, &share_unlock_cb);
		curl_share_setopt((CURLSH*)g_shareobj, CURLSHOPT_SHARE, CURL_LOCK_DATA_DNS);
	}
}

void irmacurl_global_free()
{
	if (__lockarray) {
		CRYPTO_set_locking_callback(NULL);
		int i = 0;
		for (; i < CRYPTO_num_locks(); i++)
			pthread_mutex_destroy(&__lockarray[i]);
		OPENSSL_free(__lockarray);
		__lockarray = NULL;
	}
	if (g_shareobj) {
		curl_share_cleanup((CURLSH*)g_shareobj);
		g_shareobj = NULL;
	}
	curl_global_cleanup();
}
