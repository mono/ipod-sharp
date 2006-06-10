MCS_FLAGS = -debug

TOOLS = ipod-tool.exe dmg-iso-extract.exe

IPOD_TOOL_SOURCES = $(srcdir)/PodTool.cs
DMG_ISO_EXTRACT_SRC = \
	$(srcdir)/DmgIsoExtract.cs \
	$(top_builddir)/src/Firmware/Dmg*.cs

all: $(TOOLS)

ipod-tool.exe: $(IPOD_TOOL_SOURCES)
	$(MCS) $(MCS_FLAGS) -r:$(top_builddir)/src/ipod-sharp.dll -target:exe -out:$@ $(IPOD_TOOL_SOURCES)

dmg-iso-extract.exe: $(DMG_ISO_EXTRACT_SRC)
	$(MCS) $(MCS_FLAGS) -r:ICSharpCode.SharpZipLib -target:exe -out:$@ $(DMG_ISO_EXTRACT_SRC)

EXTRA_DIST = $(IPOD_TOOL_SOURCES) $(DMG_ISO_EXTRACT_SRC)
CLEANFILES = $(TOOLS)
MAINTAINERCLEANFILES = Makefile.in

