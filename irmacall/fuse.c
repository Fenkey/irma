#include "fuse.h"

#define FUSE_VALID_TW		3600
#define FUSE_RISK_TW		600
#define FUSE_ABSOLUTE_MAX	12
#define FUSE_RATE_MIN		6
#define FUSE_RT				0.94 /* About (16/17) */

static void fuse_reset(fuse_t *f, int fuse_times_reset)
{
	f->risk_times = 0L;
	f->risk_times_save = 0L;
	f->safe_times = 0L;
	f->risk_lasttime = 0;
	f->fuse_time = 0;
	if (fuse_times_reset)
		f->fuse_times = 0;
}

int fuse_evaluate_in(fuse_t *f, time_t req_lasttime, long error_times, long fatal_times)
{
	if (!f->fuse_time) {
		if (f->risk_lasttime > 0 && req_lasttime - f->risk_lasttime > FUSE_VALID_TW)
			fuse_reset(f, 1);
		goto __ok;
	}
	if (req_lasttime - f->fuse_time > FUSE_RISK_TW * f->fuse_times) {
		fuse_reset(f, 0);
		goto __ok;
	}
	return -1;
__ok:
	f->risk_times_save = error_times + fatal_times;
	return 0;
}

void fuse_evaluate_out(fuse_t *f, time_t req_lasttime, long error_times, long fatal_times)
{
	if (f->fuse_time > 0)
		return;
	if (error_times + fatal_times == f->risk_times_save) {
		f->safe_times++;
		return;
	}
	f->risk_times++;
	f->risk_lasttime = req_lasttime;
	if (f->safe_times == 0L) {
		if (f->risk_times > FUSE_ABSOLUTE_MAX)
			goto __fuse;
	} else if (f->risk_times > FUSE_RATE_MIN) {
		float rt = (float)f->risk_times / (float)(f->risk_times + f->safe_times);
		if (rt > FUSE_RT)
			goto __fuse;
	}
	return;
__fuse:
	f->fuse_time = time(NULL);
	f->fuse_times++;
}
