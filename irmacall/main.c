#include <string.h>
#include "../config.h"
#include "service.h"

#define VERSION "0.8"

extern sopt_t mono_sopt;

static struct sopt_item {
	const char *name;
	sopt_t *sopt;
} __sopt_set[] = {
	{ "mono", &mono_sopt }
};

static int usage(service_t *s)
{
	s->console_printf( \
	" ___ ____  __  __    _    ____      _ _                                                                             \n"
	"|_ _|  _ \\|  \\/  |  / \\  / ___|__ _| | |                                                                         \n"
	" | || |_) | |\\/| | / _ \\| |   / _` | | |                                                                          \n"
	" | ||  _ <| |  | |/ ___ \\ |__| (_| | | |                                                                           \n"
	"|___|_| \\_\\_|  |_/_/   \\_\\____\\__,_|_|_|                                                                       \n"
	"+------------------------------------------------------------------------------------------------------------------+\n"
	"| Usage: irmacall [-t <log-type>] [-x <thread-count>] [-m <module-invoke>] [-c <config-of-module>] [-k] [-v] [-h]  |\n"
	"| Options:                                                                                                         |\n"
	"|    -t: Log lever of 'debug', 'event', 'warn', 'error' or 'fatal'                                                 |\n"
	"|    -x: Threads count of every process                                                                            |\n"
	"|    -m: Module invoking. Normally, it's a .Net DLL                                                                |\n"
	"|    -c: Configuration file of module                                                                              |\n"
	"|    -k: Mock request support                                                                                      |\n"
	"|    -v: Version of irmacall                                                                                       |\n"
	"|    -h: Help information                                                                                          |\n"
	"+------------------------------------------------------------------------------------------------------------------+\n");
	return -1;
}

static int version(service_t *s)
{
	char buf[128] = { 0 };
	strcat(buf, " fetcher");
	strcat(buf, " fuse");
	#ifdef SUPPORT_C_ARES
	strcat(buf, " c_ares");
	#endif
	#ifdef SUPPORT_REDIS
	strcat(buf, " memcached");
	#endif
	#ifdef SUPPORT_REDIS
	strcat(buf, " redis");
	#endif
	#ifdef SUPPORT_SMTP
	strcat(buf, " smtp");
	#endif
	s->console_printf("irmacall %s\nFeatures:%s\n", VERSION, buf);
	return -1;
}

static sopt_t* get_sopt(const char *name)
{
	int i = 0;
	for (; i < sizeof(__sopt_set)/sizeof(struct sopt_item); i++)
		if (!strcmp(__sopt_set[i].name, name))
			return __sopt_set[i].sopt;
	return NULL;
}

int main(int argc, char *argv[])
{
	service_t s;
	memset(&s, 0, sizeof(service_t));
	s.irmacall_ver = VERSION;
	s.usage = usage;
	s.version = version;
	s.sopt = get_sopt("mono");
	return service_run(argc, argv, &s);
}
