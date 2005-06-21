
MCS_FLAGS = -g

ASSEMBLY = ipod-sharp-ui.dll

ASSEMBLY_SOURCES =			\
	$(srcdir)/AssemblyInfo.cs	\
	$(srcdir)/ProgressDialog.cs

all: $(ASSEMBLY) 

$(ASSEMBLY): $(ASSEMBLY_SOURCES) $(top_builddir)/src/ipod-sharp.dll
	$(MCS) -keyfile:$(top_srcdir)/ipod-sharp.snk -target:library -o $@ $(ASSEMBLY_SOURCES) -r:$(top_builddir)/src/ipod-sharp.dll -pkg:gtk-sharp-2.0

install-data-hook:
	$(GACUTIL) /i $(ASSEMBLY) /package $(PACKAGE) /gacdir $(libdir) /root $(DESTDIR)$(libdir)

EXTRA_DIST =			\
	$(ASSEMBLY_SOURCES)

CLEANFILES = $(ASSEMBLY)
