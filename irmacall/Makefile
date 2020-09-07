CFLAGS = -Wall -Wno-unused-function -Wno-unused-result -D_GNU_SOURCE
ifeq ($(strip $(DEBUG)), yes)
CFLAGS += -g -g3
STRIP =
else
#
# 建议-O3优化，但特殊情况下不一定适用，例如将导致libmemcached联编
# 部分（c++）运行期abort（IRMAKit应用以CMemecached组件set/get验证）
#
#CFLAGS += -O3
CFLAGS += -O3
STRIP = objcopy -S $@
endif

-include ../config.in

SOURCE_EXP =
# 注意lib依赖顺序（依赖库放置在后面）：libcurl、libssl、libcrypto、libz
LIBS =
LIBS += -I$(LOCATION_MONO)/include/mono-2.0 $(LOCATION_MONO)/lib/libmono-2.0.a
LIBS += -I$(LOCATION_FCGI)/include $(LOCATION_FCGI)/lib/libfcgi.a
# libcurl由于采用了mime相关API，要求版本在v7.56.0及以上
LIBS += -I$(LOCATION_CURL)/include $(LOCATION_CURL)/lib/libcurl.a

ifeq ($(strip $(SUPPORT_C_ARES)), y)
LIBS += -I$(LOCATION_C_ARES)/include $(LOCATION_C_ARES)/lib/libcares.a
endif

ifeq ($(strip $(SUPPORT_MEMCACHED)), y)
LIBS += -I$(LOCATION_MEMCACHED)/include $(LOCATION_MEMCACHED)/lib/libmemcached.a -lstdc++
else
SOURCE_EXP += | grep -v memcached | grep -v quicklz
endif

ifeq ($(strip $(SUPPORT_REDIS)), y)
LIBS += -I$(LOCATION_HIREDIS)/include/hiredis $(LOCATION_HIREDIS)/lib/libhiredis.a
else
SOURCE_EXP += | grep -v redis
endif

ifeq ($(strip $(SUPPORT_SMTP)), n)
SOURCE_EXP += | grep -v smtp
endif

LIBS += -I$(LOCATION_SSL)/include $(LOCATION_SSL)/lib/libssl.a $(LOCATION_SSL)/lib/libcrypto.a
LIBS += -lm -lrt -lz -ldl -lpthread

TARGET = irmacall mono/cs/linux/IRMACore.dll mono/cs/windows/IRMACore-windows.dll
all: $(TARGET)

irmacall: ../config.h $(shell find . -name "*.c")
	$(CC) $(CFLAGS) -o $@ `find . -name "*.c" $(SOURCE_EXP)` $(LIBS); $(STRIP)

mono/cs/linux/IRMACore.dll: mono/cs/linux/ICall.cs mono/cs/Net.cs
	$(MCS) -sdk:$(NETSDK) -target:library -out:$@ $^

mono/cs/windows/IRMACore-windows.dll: mono/cs/windows/ICall.cs mono/cs/Net.cs
	$(MCS) -sdk:$(NETSDK) -target:library -out:$@ $^ -r:System.Web -r:System.Net -r:System.Net.Http -r:System.IO.Compression

install:
	@echo "Installing call binaries in $(BINDIR) ..."
	cp irmacall irma-launch irma-stop irma-reload irma-xlogbase irma-runtime irma-top $(BINDIR)/

clean:
	rm $(TARGET)
