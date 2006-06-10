MCS_FLAGS = -debug
ASSEMBLY = ipod-sharp-firmware.dll

ipoddir = $(prefix)/lib/ipod-sharp
ipod_DATA = $(ASSEMBLY)

ASSEMBLY_SOURCES = \
	$(srcdir)/DmgImage.cs \
	$(srcdir)/DmgPartition.cs \
	$(srcdir)/CpioArchive.cs \
	$(srcdir)/DmgFirmwareExtract.cs

all: $(ASSEMBLY) 

$(ASSEMBLY): $(ASSEMBLY_SOURCES) 
	$(MCS) $(MCS_FLAGS) -keyfile:$(top_srcdir)/ipod-sharp.snk -target:library -out:$@ $(ASSEMBLY_SOURCES) -r:ICSharpCode.SharpZipLib -r:Mono.Posix

EXTRA_DIST = $(ASSEMBLY_SOURCES)	
	
CLEANFILES = $(ASSEMBLY) *.mdb
MAINTAINERCLEANFILES = Makefile.in

