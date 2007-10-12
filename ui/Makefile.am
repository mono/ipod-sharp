
MCS_FLAGS = -debug

ASSEMBLY = ipod-sharp-ui.dll

ipodsharpdir = $(prefix)/lib/ipod-sharp
ipodsharp_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb

ASSEMBLY_SOURCES =			\
	$(srcdir)/AssemblyInfo.cs	\
	$(srcdir)/ArtworkHelpers.cs	\
	$(srcdir)/ProgressDialog.cs	\
	$(srcdir)/DeviceCombo.cs

ASSEMBLY_RESOURCES = -resource:$(srcdir)/ipod.gif

all: $(ASSEMBLY) 

$(ASSEMBLY): $(ASSEMBLY_SOURCES) $(top_builddir)/src/ipod-sharp.dll
	$(MCS) -unsafe -debug -keyfile:$(top_srcdir)/ipod-sharp.snk -target:library -out:$@ $(ASSEMBLY_SOURCES) -r:$(top_builddir)/src/ipod-sharp.dll -pkg:gtk-sharp-2.0 $(ASSEMBLY_RESOURCES)

EXTRA_DIST =			\
	ipod.gif		\
	$(ASSEMBLY_SOURCES)

CLEANFILES = $(ASSEMBLY)
MAINTAINERCLEANFILES = Makefile.in

