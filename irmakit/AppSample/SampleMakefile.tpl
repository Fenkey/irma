SDK = 4.5

all: Bin/Debug/${appName}.dll Bin/Release/${appName}.dll

Bin/Debug/${appName}.dll: ${sourceFiles}
	mkdir -p Bin
	mkdir -p Bin/Debug
	cp -d Lib/*.dll Bin/Debug
	ln -s `pwd`/Bin/Debug/MySql.Data-6.6.7.0.dll `pwd`/Bin/Debug/MySql.Data.dll
	ln -s `pwd`/Bin/Debug/zxing-0.16.5.0.dll `pwd`/Bin/Debug/zxing.dll
	mcs -sdk:$(SDK) -debug -warn:0 -define:__TESTCASE__ -target:library -out:$@ $^ @irmakit.rsp

Bin/Release/${appName}.dll: ${sourceFiles}
	mkdir -p Bin
	mkdir -p Bin/Release
	cp -d Lib/*.dll Bin/Release
	ln -s `pwd`/Bin/Release/MySql.Data-6.6.7.0.dll `pwd`/Bin/Release/MySql.Data.dll
	ln -s `pwd`/Bin/Release/zxing-0.16.5.0.dll `pwd`/Bin/Release/zxing.dll
	mcs -sdk:$(SDK) -warn:0 -target:library -out:$@ $^ @irmakit.rsp

clean:
	rm -rf Bin
