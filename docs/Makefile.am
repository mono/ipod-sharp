DOC_NAME = ipod-sharp-docs
DOC_UPDATER = @MONODOCER@ -delete
DOC_ASSEMBLER = @MDASSEMBLER@ --out $(DOC_NAME) --ecma

ASSEMBLIES = \
	$(top_builddir)/src/ipod-sharp.dll

ASSEMBLIES_BUILD = $(foreach asm,$(ASSEMBLIES),$(addprefix -assembly:,$(asm)))

if BUILD_DOCS

monodocdir = $(DOCDIR)
monodoc_DATA = \
	$(DOC_NAME).zip \
	$(DOC_NAME).tree \
	$(DOC_NAME).source

$(DOC_NAME).zip $(DOC_NAME).tree: $(srcdir)/en/*/*.xml $(srcdir)/en/*.xml
	$(DOC_ASSEMBLER) $(srcdir)/en

update-docs: $(ASSEMBLIES)
	$(DOC_UPDATER) $(ASSEMBLIES_BUILD) -path:en/

update-svn:
	@for remove in $$(find en -iregex .*\.remove$$); do \
		real_remove=$${remove%.remove}; \
		mv $$remove $$real_remove; \
		svn delete $$real_remove; \
	done; \
	for add in $$(svn status | grep ^? | awk '{print $$2}'); do \
		svn add $$add; \
	done;

endif

EXTRA_DIST = \
	$(srcdir)/en/*/*.xml \
	$(srcdir)/en/*.xml \
	$(DOC_NAME).source
	
DISTCLEANFILES = \
	$(DOC_NAME).zip \
	$(DOC_NAME).tree

MAINTAINERCLEANFILES = \
	Makefile.in

