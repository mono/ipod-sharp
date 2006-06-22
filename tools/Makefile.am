MCS_FLAGS = -debug

TOOLS = ipod-tool.exe dmg-iso-extract.exe ipod-firmware-extract.exe photo-adder.exe

IPOD_TOOL_SOURCES = $(srcdir)/PodTool.cs
DMG_ISO_EXTRACT_SRC = \
	$(srcdir)/DmgIsoExtract.cs \
	$(top_builddir)/src/Firmware/DmgImage.cs \
	$(top_builddir)/src/Firmware/DmgPartition.cs

DMG_FIRMWARE_EXTRACT_SRC = \
	$(srcdir)/ExtractFirmware.cs \
	$(top_builddir)/src/Firmware/*.cs

PHOTO_ADDER_SRC = $(srcdir)/PhotoAdder.cs

all: $(TOOLS)

ipod-tool.exe: $(IPOD_TOOL_SOURCES)
	$(MCS) $(MCS_FLAGS) -r:$(top_builddir)/src/ipod-sharp.dll -target:exe -out:$@ $(IPOD_TOOL_SOURCES)

dmg-iso-extract.exe: $(DMG_ISO_EXTRACT_SRC)
	$(MCS) $(MCS_FLAGS) -r:ICSharpCode.SharpZipLib -target:exe -out:$@ $(DMG_ISO_EXTRACT_SRC)

ipod-firmware-extract.exe: $(DMG_FIRMWARE_EXTRACT_SRC)
	$(MCS) $(MCS_FLAGS) -r:ICSharpCode.SharpZipLib -target:exe -out:$@ $(DMG_FIRMWARE_EXTRACT_SRC)

photo-adder.exe: $(PHOTO_ADDER_SRC)
	$(MCS) $(MCS_FLAGS) -target:exe -out:$@ $(PHOTO_ADDER_SRC) -r:$(top_builddir)/src/ipod-sharp.dll -r:$(top_builddir)/ui/ipod-sharp-ui.dll -pkg:gtk-sharp-2.0

EXTRA_DIST = $(IPOD_TOOL_SOURCES) $(DMG_ISO_EXTRACT_SRC) $(DMG_FIRMWARE_EXTRACT_SRC) $(PHOTO_ADDER_SRC)
CLEANFILES = $(TOOLS) *.mdb
MAINTAINERCLEANFILES = Makefile.in

