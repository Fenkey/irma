TARGET = Bin/IRMAKit.dll Bin/IRMAKit-windows.dll Bin/AppGenerator.exe
all: $(TARGET)

Bin/IRMAKit.dll: Properties/*.cs Log/*.cs Configure/*.cs Web/*.cs Web/Attributes/*.cs Web/Attributes/*/*.cs Store/*.cs Templates/*.cs Utils/*.cs
	mkdir -p Bin
	cp -d Lib/*.dll Bin
	mcs -sdk:$(NETSDK) -warn:0 -target:exe -out:$@ $^ @irmakit.rsp

Bin/IRMAKit-windows.dll: Properties/*.cs Log/*.cs Configure/*.cs Web/*.cs Web/Attributes/*.cs Web/Attributes/*/*.cs Store/*.cs Templates/*.cs Utils/*.cs
	mkdir -p Bin
	cp -d Lib/*.dll Bin
	mcs -sdk:$(NETSDK) -warn:0 -target:exe -out:$@ $^ @irmakit-windows.rsp

Bin/AppGenerator.exe: AppGenerator/AppGenerator.cs
	mkdir -p Bin
	mcs -sdk:$(NETSDK) -warn:0 -target:exe -out:$@ $^ -r:Bin/IRMAKit.dll @irmakit.rsp

install:
	@echo "Installing kit libraries on $(ASSEMBLYDIR) ..."
	cp ../irmacall/mono/cs/linux/IRMACore.dll ../irmacall/mono/cs/windows/IRMACore-windows.dll $(ASSEMBLYDIR)/
	cp Bin/*.dll $(ASSEMBLYDIR)/
	cp -a Bin/AppGenerator.exe AppSample $(ASSEMBLYDIR)/
	cp AppGenerator/irma-genapp $(BINDIR)/

clean:
	rm -rf Bin
