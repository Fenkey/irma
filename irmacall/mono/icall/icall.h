#ifndef __ICALL_H__
#define __ICALL_H__

#include <unistd.h>
#include <stdio.h>
#include <string.h>
#include <mono/jit/jit.h>
#include <mono/metadata/object.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/mono-gc.h>
#include "../../buf.h"
#include "../../map.h"
#include "../../log.h"
#include "../../misc.h"
#include "../../unit.h"
#include "../../smtp.h"
#include "../../fetcher.h"
#include "../../kvstore.h"
#include "../../wrapper.h"
#include "../../service.h"

#ifdef __cplusplus
extern "C" {
#endif

typedef struct {
	worker_t	*worker;		/* 工作线程对象反向索引 */
	buf_t		*buf;			/* 内部使用buffer */
	map_t		*map;			/* 对象字典，所有扩展的功能或数据信息均适合放置在map内，特别是非必备的、非唯一的功能或信息 */
	fetcher_t	*fetcher;		/* HTTP请求器 */
	MonoDomain	*domain;		/* mono域对象 */
	MonoThread	*thread;		/* mono托管主线程 */
	MonoObject	*obj_service;	/* irmakit service对象 */
	MonoObject	*obj_global;	/* irmakit 安全的线程内静态化全局对象 */
	int			handle_locked;	/* 进入框架层Handle()之前是否已lock */

	int		(*init)(worker_t *w);
	int		(*handle)(worker_t *w);
	int		(*reload)(worker_t *w);
	void	(*keepalive)(worker_t *w);
	void	(*finalize)(worker_t *w);
	void	(*handle_unlock)(worker_t *w);
} app_t;

#define mono_array_in_b(array,alen,buf,blen) \
do { \
	buf_data_reset(buf, blen); \
	memcpy((buf)->data, mono_array_addr(array, unsigned char, 0), alen); \
	(buf)->offset = alen; \
	(buf)->data[alen] = 0; \
} while (0)

#define mono_array_in(array,len,buf) \
mono_array_in_b(array,len,buf,len)

#define mono_array_out(array,p,len) \
do { \
	if ((p) != NULL && (len) > 0) \
		memcpy(mono_array_addr(array, unsigned char, 0), p, len); \
} while (0)

#define mono_array_out_b(array,buf) \
mono_array_out(array,(buf)->data,(buf)->offset)

typedef struct {
	const char	*i_name;
	const void	*i_method;
} icall_item_t;

#define ICALL_ITEM(n,m)	{"IRMACore.Lower.ICall::"#n, (const void*)&(m)}
#define ICALL_ITEM_NULL	{NULL, NULL}
#define regit(items)	do { icall_item_t *p = items; for (; p && p->i_name; p++) mono_add_internal_call(p->i_name, p->i_method); } while (0)

void register_icall();

#ifdef __cplusplus
}
#endif

#endif
