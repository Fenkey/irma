#include "icall.h"

static void launch_info(MonoString *appname, MonoString *version, long bodyMax, MonoString *url)
{
	worker_t *w = CURRENT;
	char *a = mono_string_to_utf8(appname);
	char *v = mono_string_to_utf8(version);
	char *u = url ? mono_string_to_utf8(url) : NULL;
	w->launch_info(w, a, v, bodyMax, u);
	if (a) mono_free(a);
	if (v) mono_free(v);
	if (u) mono_free(u);
}

static void launched()
{
	worker_t *w = CURRENT;
	w->launched(w);
}

static MonoString* get_os()
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;
	return mono_string_new(app->domain, "linux");
}

static int get_worker_index()
{
	return CURRENT->index;
}

static long get_unixtime()
{
	return time(NULL);
}

static long build_unixtime(int y, int m, int d, int h, int M, int s)
{
	return sometime(y, m, d, h, M, s);
}

static long build_gmtime(MonoString *str)
{
	char *p = mono_string_to_utf8(str);
	if (!p)
		return -1;
	static char *months[] = {"Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"};
	char M[4];
	int d, y, h, m, s;
	int ret = sscanf(p, "%*[^ ] %02d %s %04d %02d:%02d:%02d GMT", &d, M, &y, &h, &m, &s);
	mono_free(p);
	if (ret != 6)
		return -1;
	int i = 0;
	for (; i < 12; i++) {
		if (!strcmp(months[i], M))
			break;
	}
	if (i >= 12)
		return -1;
	struct tm t = {.tm_mday=d, .tm_mon=i, .tm_year=y-1900, .tm_hour=h, .tm_min=m, .tm_sec=s};
	return mktime(&t);
}

static long unixtime2gmtime(long lt, MonoString **str)
{
	struct tm tm;
	if (!gmtime_r((time_t*)&lt, &tm))
		return -1;
	if (str) {
		char buf[32];
		worker_t *w = CURRENT;
		app_t *app = (app_t*)w->priv_app;
		strftime(buf, sizeof(buf), "%a, %d %b %Y %H:%M:%S GMT", &tm);
		*str = mono_string_new(app->domain, buf);
	}
	return mktime(&tm);
}

static MonoString* get_current_path()
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;

	char cwd[256];
	return mono_string_new(app->domain, getcwd(cwd, sizeof(cwd)));
}

static MonoString* shell_execute(MonoString *cmd)
{
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;

	char *p = mono_string_to_utf8(cmd);
	if (!p)
		return mono_string_new(app->domain, "");

	FILE *f = popen(p, "r");
	mono_free(p);
	buf_reset(app->buf);
	if (f) {
		char line[2048];
		while (fgets(line, sizeof(line), f) != NULL)
			buf_append(app->buf, line, strlen(line));
		pclose(f);
	}
	MonoString *ret;
	if (app->buf->offset > 0) {
		ret = mono_string_new(app->domain, app->buf->data);
		buf_force_reset(app->buf);
	} else
		ret = mono_string_new(app->domain, "");
	return ret;
}

static icall_item_t __items[] = {
	ICALL_ITEM(LaunchInfo, launch_info),
	ICALL_ITEM(Launched, launched),
	ICALL_ITEM(GetOS, get_os),
	ICALL_ITEM(GetWorkerIndex, get_worker_index),
	ICALL_ITEM(GetUnixTime, get_unixtime),
	ICALL_ITEM(BuildUnixTime, build_unixtime),
	ICALL_ITEM(BuildGmTime, build_gmtime),
	ICALL_ITEM(UnixTimeToGmTime, unixtime2gmtime),
	ICALL_ITEM(GetCurrentPath, get_current_path),
	ICALL_ITEM(ShellExecute, shell_execute),
	ICALL_ITEM_NULL
};

void reg_sys() { regit(__items); }
