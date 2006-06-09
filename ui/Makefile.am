
MCS_FLAGS = -debug

ASSEMBLY = ipod-sharp-ui.dll

ipodsharpdir = $(prefix)/lib/ipod-sharp
ipodsharp_DATA = $(ASSEMBLY)

ASSEMBLY_SOURCES =			\
	$(srcdir)/AssemblyInfo.cs	\
	$(srcdir)/ProgressDialog.cs	\
	$(srcdir)/DeviceCombo.cs

ASSEMBLY_RESOURCES = -resource:$(srcdir)/ipod.gif

all: $(ASSEMBLY) 

$(ASSEMBLY): $(ASSEMBLY_SOURCES) $(top_builddir)/src/ipod-sharp.dll
	$(MCS) -keyfile:$(top_srcdir)/ipod-sharp.snk -target:library -out:$@ $(ASSEMBLY_SOURCES) -r:$(top_builddir)/src/ipod-sharp.dll -pkg:gtk-sharp-2.0 $(ASSEMBLY_RESOURCES)

#install-data-hook:
#	$(GACUTIL) /i $(ASSEMBLY) /package $(PACKAGE) /gacdir $(libdir) /root $(DESTDIR)$(libdir)

EXTRA_DIST =			\
	ipod.gif		\
	$(ASSEMBLY_SOURCES)

CLEANFILES = $(ASSEMBLY)
MAINTAINERCLEANFILES = Makefile.in

