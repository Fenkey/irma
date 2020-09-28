#include <unistd.h>
#include <assert.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <libgen.h>
#include <pthread.h>
#include "../service.h"
#include "icall/icall.h"

#define IRMAROOT	"IRMAROOT"
#define IRMAKIT_DLL	"IRMAKit.dll"

static char *__config = NULL;
static char __irmakit_dll[256];
static char __app_dll[256];
static char __namespace[64];
static char __classname[32];

static MonoDomain *__mono_domain = NULL;
static MonoClass *__mono_app_klass = NULL;
static MonoMethod *__mono_app_init = NULL;
static MonoMethod *__mono_app_handle = NULL;
static MonoMethod *__mono_app_keepalive = NULL;
static MonoMethod *__mono_app_reload = NULL;
static MonoMethod *__mono_app_finalize = NULL;

static pthread_mutex_t __handle_mutex = PTHREAD_MUTEX_INITIALIZER;
static pthread_mutex_t __keepalive_mutex = PTHREAD_MUTEX_INITIALIZER;
static pthread_mutex_t __finalize_mutex = PTHREAD_MUTEX_INITIALIZER;

static int set_workplace(int (*g_log_build)())
{
	if (strchr(__app_dll, '/')) {
		char *p = xstrdup(__app_dll);
		int ret = chdir(dirname(p));
		free(p);
		if (ret == 0)
			return g_log_build();
	}
	return -1;
}

static int check_module(const char *binary_path, const char *module_str, int (*g_log_build)())
{
	/* module format: "<namespace>:<classname>,dll" */
	if (sscanf(module_str, "%[^:]:%[^,],%s", __namespace, __classname, __app_dll) != 3)
		return -1;
	if (set_workplace(g_log_build) < 0)
		return -1;
	/* We can log something from now on ! */
	if (access(__app_dll, F_OK|R_OK) < 0) {
		FATAL(g_log, "Core - Dll not found: %s", __app_dll);
		return -1;
	}

	/* Search IRMAKit.dll by __app_dll. */
	strcpy(__irmakit_dll, __app_dll);
	char *p = strrchr(__irmakit_dll, '/');
	if (!p)
		p = __irmakit_dll;
	sprintf(p, "/%s", IRMAKIT_DLL);
	if (!access(__irmakit_dll, F_OK|R_OK))
		return 0;

	/* Search it by the environment variable IRMAROOT. */
	if ((p = getenv(IRMAROOT)) != NULL) {
		strcpy(__irmakit_dll, p);
		int len = strlen(__irmakit_dll);
		p = &__irmakit_dll[len-1];
		if (*p == '/')
			*p = '\0';
		sprintf(p, "/assembly/%s", IRMAKIT_DLL);
		if (!access(__irmakit_dll, F_OK|R_OK))
			return 0;
	}

	/* Search it by binary_path that in usually is irmacall. */
	int n = readlink(binary_path, __irmakit_dll, sizeof(__irmakit_dll));
	if (n > 0 && n < sizeof(__irmakit_dll))
		__irmakit_dll[n] = '\0';
	else if (strstr(binary_path, "/"))
		strcpy(__irmakit_dll, binary_path);
	else {
		char cmd[64];
		snprintf(cmd, sizeof(cmd), "which %s", binary_path);
		FILE *f = popen(cmd, "r");
		if (!f)
			goto __wrong;
		p = fgets(__irmakit_dll, sizeof(__irmakit_dll), f);
		pclose(f);
		if (!p)
			goto __wrong;
	}
	p = strstr(__irmakit_dll, "/irma/bin/");
	if (p) {
		sprintf(p, "/irma/assembly/%s", IRMAKIT_DLL);
		if (!access(__irmakit_dll, F_OK|R_OK))
			return 0;
	}

__wrong:
	FATAL(g_log, "Core - Dll not found: %s", IRMAKIT_DLL);
	return -1;
}

static int opt_parse(int argc, char *argv[], service_t *s, int (*g_log_build)())
{
	if (argc < 2)
		return s->usage(s);

	int c;
	char *p;
	while ((c = getopt(argc, argv, "t:x:m:c:kvh")) != -1) {
		switch (c) {
		case 't':
			p = optarg;
			if (!strcasecmp(p, "debug"))
				s->logbase = LT_DEBUG;
			else if (!strcasecmp(p, "event"))
				s->logbase = LT_EVENT;
			else if (!strcasecmp(p, "warn"))
				s->logbase = LT_WARN;
			else if (!strcasecmp(p, "error"))
				s->logbase = LT_ERROR;
			else if (!strcasecmp(p, "fatal"))
				s->logbase = LT_FATAL;
			break;

		case 'x':
			s->thread_count = atoi(optarg);
			if (s->thread_count <= 0)
				return s->usage(s);
			break;

		case 'm':
			if (check_module(argv[0], optarg, g_log_build) < 0)
				return s->usage(s);
			break;

		case 'c':
			__config = xstrdup(optarg);
			break;

		case 'k':
			s->mock_support = 1;
			break;

		case 'v':
			return s->version(s);

		default:
			return s->usage(s);
		}
	}

	return 0;
}

static MonoMethod* find_method(MonoClass *klass, const char *method_name, int param_count)
{
	MonoMethod *m = NULL;
	MonoClass *cls = klass;
	while (cls) {
		m = mono_class_get_method_from_name(cls, method_name, param_count);
		if (m)
			break;
		cls = mono_class_get_parent(cls);
	}
	return m;
}

static int common_init(int thread_count, int argc, char *argv[])
{
	/*
	 * Compile IRMAKit.dll in the way of 'exe' so that it can be performed
	 * by mono_jit_exec() to get an reliable domain.
	 */
	__mono_domain = mono_jit_init(__irmakit_dll);
	if (!__mono_domain) {
		FATAL(g_log, "Core - Invalid dll: %s", __irmakit_dll);
		goto __wrong;
	}

	MonoAssembly *assembly = mono_domain_assembly_open(__mono_domain, __irmakit_dll);
	if (!assembly) {
		FATAL(g_log, "Core - Invalid assembly of dll: %s", __irmakit_dll);
		goto __wrong;
	}
	mono_jit_exec(__mono_domain, assembly, argc, argv);

	assembly = mono_domain_assembly_open(__mono_domain, __app_dll);
	if (!assembly) {
		FATAL(g_log, "Core - Invalid assembly of dll: %s", __app_dll);
		goto __wrong;
	}

	MonoImage *image = (MonoImage*)mono_assembly_get_image(assembly);
	if (!image) {
		FATAL(g_log, "Core - Invalid image of dll: %s", __app_dll);
		goto __wrong;
	}

	__mono_app_klass = mono_class_from_name(image, __namespace, __classname);
	if (!__mono_app_klass) {
		FATAL(g_log, "Core - Invalid klass: %s:%s", __namespace, __classname);
		goto __wrong;
	}

	if (!(__mono_app_handle = find_method(__mono_app_klass, "Handle", 0))) {
		FATAL(g_log, "Core - Handle() is missing");
		goto __wrong;
	}

	if (!(__mono_app_init = find_method(__mono_app_klass, "Init", 2)))
		WARN(g_log, "Core - Init() is missing");

	if (!(__mono_app_keepalive = find_method(__mono_app_klass, "KeepAlive", 1)))
		WARN(g_log, "Core - KeepAlive() is missing");

	if (!(__mono_app_finalize = find_method(__mono_app_klass, "Finalize", 0)))
		WARN(g_log, "Core - Finalize() is missing");

	__mono_app_reload = find_method(__mono_app_klass, "Reload", 2);

	register_icall();
	return 0;

__wrong:
	if (__mono_domain)
		mono_jit_cleanup(__mono_domain);
	return -1;
}

static int app_mono_init(worker_t *w)
{
	/*
	 * This process may not be suitable for race start with unmanaged thread lock. For example,
	 * creating MySQL objects at this stage may lead to segment exception (SIGSEGV) due to static
	 * links and other reasons. So for the security reasons, you'd better invoke app_mono_init()
	 * iteratively (launch_next).
	 */
	app_t *app = (app_t*)w->priv_app;
	void *args[] = { mono_string_new(app->domain, __config), &app->obj_global };
	MonoObject *e = NULL;
	mono_runtime_invoke(__mono_app_init, app->obj_service, args, &e);
	if (e) {
		FATAL(w->log, "Core - Raise exception while invoking application ! Check it pls");
		return -1;
	}
	return 0;
}

static int app_mono_handle(worker_t *w)
{
	app_t *app = (app_t*)w->priv_app;
	MonoObject *e = NULL;
	mono_runtime_invoke(__mono_app_handle, app->obj_service, NULL, &e);
	/* The exception means to quit now. */
	return e ? -1 : 0;
}

static int app_mono_handle_safely(worker_t *w)
{
	int ret;
	app_t *app = (app_t*)w->priv_app;
	pthread_mutex_lock(&__handle_mutex);
	app->handle_locked = 1;
	ret = app_mono_handle(w);
	/* Switch it forever. */
	app->handle = app_mono_handle;
	return ret;
}

static void app_mono_handle_unlock(worker_t *w)
{
	app_t *app = (app_t*)w->priv_app;
	if (app->handle_locked) {
		app->handle_locked = 0;
		pthread_mutex_unlock(&__handle_mutex);
	}
}

static int app_mono_reload(worker_t *w)
{
	app_t *app = (app_t*)w->priv_app;
	void *args[] = { mono_string_new(app->domain, __config), &app->obj_global };
	MonoObject *e = NULL;
	mono_runtime_invoke(__mono_app_reload, app->obj_service, args, &e);
	if (e) {
		FATAL(w->log, "Core - Raise exception while invoking application Reload ! Check it pls");
		return -1;
	}
	return 0;
}

static void app_mono_keepalive(worker_t *w)
{
	app_t *app = (app_t*)w->priv_app;
	MonoObject *old = app->obj_global;
	void *args[] = { &app->obj_global };
	MonoObject *e = NULL;
	mono_runtime_invoke(__mono_app_keepalive, app->obj_service, args, &e);
	if (e)
		WARN(w->log, "Core - Raise exception while invoking application KeepAlive ! Check it pls");
	else if (app->obj_global != old) {
		/* nothing. */
	}
}

static void app_mono_keepalive_safely(worker_t *w)
{
	if (__mono_app_keepalive) {
		pthread_mutex_lock(&__keepalive_mutex);
		app_mono_keepalive(w);
		/* Switch it forever. */
		((app_t*)w->priv_app)->keepalive = app_mono_keepalive;
		pthread_mutex_unlock(&__keepalive_mutex);
	}
}

static void app_mono_finalize(worker_t *w)
{
	app_t *app = (app_t*)w->priv_app;
	MonoObject *e = NULL;
	mono_runtime_invoke(__mono_app_finalize, app->obj_service, NULL, &e);
	if (e)
		WARN(w->log, "Core - Raise exception while invoking application's Finalize ! Check it pls");
}

static void app_mono_finalize_safely(worker_t *w)
{
	if (__mono_app_finalize) {
		pthread_mutex_lock(&__finalize_mutex);
		app_mono_finalize(w);
		/* Switch it anyway. */
		((app_t*)w->priv_app)->finalize = app_mono_finalize;
		pthread_mutex_unlock(&__finalize_mutex);
	}
}

static int app_init(worker_t *w)
{
	app_t *app = (app_t *)xcalloc(1, sizeof(app_t));
	app->buf = w->pool->lend(w->pool, 0, 1);
	app->map = map_new();
	app->fetcher = fetcher_new(w->pool);
	app->worker = w; w->priv_app = app;
	app->domain = mono_get_root_domain();
	app->thread = mono_thread_attach(app->domain);
	app->init = app_mono_init;
	app->handle = app_mono_handle_safely;
	app->reload = app_mono_reload;
	app->keepalive = app_mono_keepalive_safely;
	app->finalize = app_mono_finalize_safely;
	app->handle_unlock = app_mono_handle_unlock;
	app->obj_service = mono_object_new(app->domain, __mono_app_klass);
	mono_runtime_object_init(app->obj_service);
	return app->init(w);
}

static int app_handle(worker_t *w)
{
	app_t *app = (app_t*)w->priv_app;
	mono_thread_attach(app->domain);
	//mono_object_get_class(app->obj_global);
	return app->handle(w);
}

static int app_reload(worker_t *w)
{
	if (!__mono_app_reload)
		return 0;
	app_t *app = (app_t*)w->priv_app;
	if (!app)
		return 0;
	if (app->map) {
		map_free(app->map);
		app->map = map_new();
	}
	return app->reload(w);
}

static int app_free(worker_t *w, int finalize)
{
	/* Try to make it recallable. */
	app_t *app = (app_t*)w->priv_app;
	if (!app)
		return -1;
	if (finalize && app->finalize) {
		app->finalize(w);
		app->finalize = NULL;
	}
	log_sync(w->log);
	if (app->thread) {
		mono_thread_detach(app->thread);
		app->thread = NULL;
	}
	if (app->map) {
		map_free(app->map);
		app->map = NULL;
	}
	if (app->fetcher) {
		fetcher_free(app->fetcher);
		app->fetcher = NULL;
	}
	if (app->buf) {
		buf_force_reset(app->buf);
		buf_return(app->buf);
		app->buf = NULL;
	}
	/*
	 * NOTE NOT mono_free(app->obj_service) !
	 * It'll be retrieved by mono GC.
	 */
	free(app);
	w->priv_app = NULL;
	return 0;
}

static int common_free(int thread_count)
{
	if (__config)
		free(__config);
	mono_jit_cleanup(__mono_domain);
	return 0;
}

static int gc()
{
	mono_thread_attach(mono_get_root_domain());
	mono_gc_collect(mono_gc_max_generation());
	return 0;
}

sopt_t mono_sopt = {
	.opt_parse = opt_parse,
	.common_init = common_init,
	.app_init = app_init,
	.app_handle = app_handle,
	.app_reload = app_reload,
	.app_free = app_free,
	.common_free = common_free,
	.gc = gc
};
