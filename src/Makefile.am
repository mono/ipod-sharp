
DOC_UPDATER = @MONODOCER@ -delete
DOC_ASSEMBLER = @MONODOC@ --assemble

MCS_FLAGS = -g

ASSEMBLY = ipod-sharp.dll

ASSEMBLY_SOURCES =			\
	$(srcdir)/AssemblyInfo.cs	\
	$(srcdir)/Device.cs		\
	$(srcdir)/DeviceModel.cs	\
	$(srcdir)/DeviceException.cs	\
	$(srcdir)/DeviceBusyException.cs	\
	$(srcdir)/Equalizer.cs		\
	$(srcdir)/Song.cs		\
	$(srcdir)/Playlist.cs		\
	$(srcdir)/SongDatabase.cs

docdir = `@MONODOC@ --get-sourcesdir`
doc_DATA =              	\
	ipod-sharp-docs.zip	\
	ipod-sharp-docs.tree	\
	ipod-sharp-docs.source

all: $(ASSEMBLY) 

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(MCS) $(MCS_FLAGS) -keyfile:$(top_srcdir)/ipod-sharp.snk -target:library -o $@ $(ASSEMBLY_SOURCES) -r:Mono.Posix -pkg:gtk-sharp-2.0

update-docs: $(ASSEMBLY)
	$(DOC_UPDATER) -assembly:$(ASSEMBLY) -path:en

ipod-sharp-docs.zip ipod-sharp-docs.tree: $(srcdir)/en/*/*.xml
	$(DOC_ASSEMBLER) --ecma $(srcdir)/en -o ipod-sharp-docs 

install-data-hook:
	$(GACUTIL) /i $(ASSEMBLY) /package $(PACKAGE) /gacdir $(libdir) /root $(DESTDIR)$(libdir)

EXTRA_DIST =			\
	$(ASSEMBLY_SOURCES)	\
	ipod-sharp.dll.config	\
	ipod-sharp.snk 		\
	$(srcdir)/en/*.xml	\
	$(srcdir)/en/*/*.xml	\
	ipod-sharp-docs.source

CLEANFILES = $(ASSEMBLY)
