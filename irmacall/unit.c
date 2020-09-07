#include <stdlib.h>
#include <string.h>
#include "unit.h"

#define USIZE_B		1
#define USIZE_K		(USIZE_B)<<10
#define USIZE_M 	(USIZE_K)<<10
#define USIZE_G 	(USIZE_M)<<10

#define UTIME_S		1
#define UTIME_M1	(UTIME_S)*60
#define UTIME_H		(UTIME_M1)*60
#define UTIME_D		(UTIME_H)*24
#define UTIME_W		(UTIME_D)*7
#define UTIME_M2	(UTIME_D)*30
#define UTIME_Y		(UTIME_D)*365

typedef enum { UNIT_SIZE = 0, UNIT_TIME, } unit_type_t;

static long unit_factor(unit_type_t type, const char *end)
{
	if (!*end)
		return 1;
	long ret = 0;

	switch (type) {
	case UNIT_SIZE:
		if (!strcasecmp(end, "b"))
			ret = USIZE_B;
		else if (!strcasecmp(end, "k"))
			ret = USIZE_K;
		else if (!strcasecmp(end, "m"))
			ret = USIZE_M;
		else if (!strcasecmp(end, "g"))
			ret = USIZE_G;
		break;

	case UNIT_TIME:
		if (!strcasecmp(end, "s"))
			ret = UTIME_S;
		else if (!strcmp(end, "m"))
			ret = UTIME_M1;
		else if (!strcasecmp(end, "h"))
			ret = UTIME_H;
		else if (!strcasecmp(end, "d"))
			ret = UTIME_D;
		else if (!strcasecmp(end, "w"))
			ret = UTIME_W;
		else if (!strcmp(end, "M"))
			ret = UTIME_M2;
		else if (!strcasecmp(end, "y"))
			ret = UTIME_Y;
		break;
	}
	return ret;
}

long parse_bytes(const char *value)
{
	if (!value)
		return -1;
	char *end;
	long v = strtol(value, &end, 0);
	long f = unit_factor(UNIT_SIZE, end);
	return f > 0 ? (v * f) : -1;
}

long parse_seconds(const char *value)
{
	if (!value)
		return -1;
	char *end;
	long v = strtol(value, &end, 0);
	long f = unit_factor(UNIT_TIME, end);
	return f > 0 ? (v * f) : -1;
}
