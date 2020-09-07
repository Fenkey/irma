#include "icall.h"

static int mail(
	MonoString *server,
	MonoString *user,
	MonoString *password,
	MonoString *subject,
	MonoString *to,
	MonoString *content,
	MonoString *a0,
	MonoString *a1,
	MonoString *a2,
	int hideto,
	MonoString **error)
{
	int ret = -1;
	worker_t *w = CURRENT;
	app_t *app = (app_t*)w->priv_app;

	char *p_server = mono_string_to_utf8(server);
	if (!p_server)
		goto __f0;
	char *p_user = mono_string_to_utf8(user);
	if (!p_user)
		goto __f1;
	char *p_password = mono_string_to_utf8(password);
	if (!p_password)
		goto __f2;
	char *p_subject = mono_string_to_utf8(subject);
	if (!p_subject)
		goto __f3;
	char *p_to = mono_string_to_utf8(to);
	if (!p_to)
		goto __f4;
	char *p_content = mono_string_to_utf8(content);
	if (!p_content)
		goto __f5;

	buf_printf(app->buf, "smtp-%s-%s", p_server, p_user);
	smtp_t *smtp = (smtp_t*)app->map->get(app->map, app->buf->data);
	if (!smtp) {
		if (!(smtp = smtp_new(w->pool, p_server, p_user, p_password)))
			goto __f6;
		app->map->set(app->map, app->buf->data, smtp, &smtp_free);
	}
	smtp->clean(smtp);

	buf_printf(smtp->to, p_to);
	buf_printf(smtp->subject, p_subject);
	buf_printf(smtp->content, p_content);
	char *p;
	if ((p = mono_string_to_utf8(a0)) != NULL) {
		buf_printf(smtp->attachment[0], p);
		mono_free(p);
	}
	if ((p = mono_string_to_utf8(a1)) != NULL) {
		buf_printf(smtp->attachment[1], p);
		mono_free(p);
	}
	if ((p = mono_string_to_utf8(a2)) != NULL) {
		buf_printf(smtp->attachment[2], p);
		mono_free(p);
	}
	ret = smtp->mail(smtp, hideto, 0);
	if (ret < 0 && *smtp->error && error)
		*error = mono_string_new(app->domain, smtp->error);

__f6:
	mono_free(p_content);
__f5:
	mono_free(p_to);
__f4:
	mono_free(p_subject);
__f3:
	mono_free(p_password);
__f2:
	mono_free(p_user);
__f1:
	mono_free(p_server);
__f0:
	return ret;
}

static icall_item_t __items[] = {
	ICALL_ITEM(SmtpMail, mail),
	ICALL_ITEM_NULL
};

void reg_smtp() { regit(__items); }
