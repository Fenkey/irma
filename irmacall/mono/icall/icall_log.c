#include "icall.h"

static void debug(MonoString *content)
{
	worker_t *w = CURRENT;
	char *p = mono_string_to_utf8(content);
	DEBUG_S(w->log, p);
	mono_free(p);
}

static void event(MonoString *content)
{
	worker_t *w = CURRENT;
	char *p = mono_string_to_utf8(content);
	EVENT_S(w->log, p);
	mono_free(p);
}

static void warn(MonoString *content)
{
	worker_t *w = CURRENT;
	char *p = mono_string_to_utf8(content);
	WARN_S(w->log, p);
	mono_free(p);
}

static void error(MonoString *content)
{
	worker_t *w = CURRENT;
	char *p = mono_string_to_utf8(content);
	ERROR_S(w->log, p);
	w->log_record(w, LT_ERROR);
	mono_free(p);
}

static void fatal(MonoString *content)
{
	worker_t *w = CURRENT;
	char *p = mono_string_to_utf8(content);
	FATAL_S(w->log, p);
	w->log_record(w, LT_FATAL);
	mono_free(p);
}

static void tc(MonoString *content)
{
	worker_t *w = CURRENT;
	char *p = mono_string_to_utf8(content);
	TC(w->log, p);
	mono_free(p);
}

static icall_item_t __items[] = {
	ICALL_ITEM(LogDebug, debug),
	ICALL_ITEM(LogEvent, event),
	ICALL_ITEM(LogWarn, warn),
	ICALL_ITEM(LogError, error),
	ICALL_ITEM(LogFatal, fatal),
	ICALL_ITEM(LogTc, tc),
	ICALL_ITEM_NULL
};

void reg_log() { regit(__items); }
