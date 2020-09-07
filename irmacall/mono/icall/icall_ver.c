#include "icall.h"

static int cmp(MonoString *v1, MonoString *v2, MonoString **error)
{
	*error = NULL;
	int ret = -1;
	char *p1 = mono_string_to_utf8(v1);
	char *p2 = mono_string_to_utf8(v2);
	if (p1 && p2)
		ret = strverscmp(p1, p2);
	else {
		app_t *app = (app_t*)CURRENT->priv_app;
		*error = mono_string_new(app->domain, "Invalid version");
	}
	if (p1)
		mono_free(p1);
	if (p2)
		mono_free(p2);
	return ret;
}

static icall_item_t __items[] = {
	ICALL_ITEM(VerCmp, cmp),
	ICALL_ITEM_NULL
};

void reg_ver() { regit(__items); }
