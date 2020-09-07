#ifndef __FUSE_H__
#define __FUSE_H__

#include <time.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef struct {
	int		fuse_times;		/* 有效期内熔断累计次数 */
	time_t	fuse_time;		/* 熔断开始时间。fuse_time > 0 标识为当前处于熔断期 */
	time_t	risk_lasttime;	/* 最后一次标记风险时间 */
	long	risk_times;		/* 检测期连续风险累计 */
	long	risk_times_save;/* 熔断检测前风险值快照 */
	long	safe_times;		/* 检测期连续安全累计 */
} fuse_t;

int fuse_evaluate_in(fuse_t *f, time_t req_lasttime, long error_times, long fatal_times);
void fuse_evaluate_out(fuse_t *f, time_t req_lasttime, long error_times, long fatal_times);

#ifdef __cplusplus
}
#endif

#endif
