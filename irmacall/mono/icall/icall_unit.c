#include "icall.h"

static long __parse_bytes(MonoString *val)
{
	char *v = mono_string_to_utf8(val);
	if (!v)
		return 0L;
	long ret = parse_bytes(v);
	mono_free(v);
	return ret;
}

static long __parse_seconds(MonoString *val)
{
	char *v = mono_string_to_utf8(val);
	if (!v)
		return 0L;
	long ret = parse_seconds(v);
	mono_free(v);
	return ret;
}

static icall_item_t __items[] = {
	ICALL_ITEM(UnitParseBytes, __parse_bytes),
	ICALL_ITEM(UnitParseSeconds, __parse_seconds),
	ICALL_ITEM_NULL
};

void reg_unit() { regit(__items); }
