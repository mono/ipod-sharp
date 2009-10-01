SUBDIRS = src ui docs

#if ENABLE_TESTS
#SUBDIRS += tests
#endif

pkgconfigdir = $(libdir)/pkgconfig
pkgconfig_DATA = ipod-sharp.pc ipod-sharp-ui.pc

EXTRA_DIST = ipod-sharp.snk

DISTCLEANFILES = \
	ipod-sharp.pc \
	ipod-sharp-ui.pc \
	*.bak \
	*~ \
	*.pidb

MAINTAINERCLEANFILES = \
	compile \
	INSTALL \
	config.h.in \
	aclocal.m4 \
	ltmain.sh \
	Makefile.in \
	depcomp \
	missing \
	install-sh \
	configure \
	config.sub \
	config.guess \
	intltool-extract.in \
	intltool-merge.in   \
	intltool-update.in  \
	mkinstalldirs

# ---------------
# Release Targets
# ---------------

-include release-rc

prepare-release: distcheck
	rm -rf release-data
	mkdir release-data
	cp NEWS release-data/$(PACKAGE)-$(VERSION).news
	cp ChangeLog release-data/$(PACKAGE)-$(VERSION).changes
	find -maxdepth 1 \( -name \*.zip -o -name \*.bz2 -o -name \*.gz \) -exec cp -a {} release-data \;
	cd release-data && sha256sum * > $(PACKAGE)-$(VERSION).sha256sum

upload-release:
	scp -r release-data $(RELEASE_WEB_USER)@$(RELEASE_WEB_HOST):$(RELEASE_WEB_PATH)/$(VERSION)
	ssh $(RELEASE_WEB_USER)@$(RELEASE_WEB_HOST) rm -f $(RELEASE_WEB_PATH)/LATEST-IS\*
	ssh $(RELEASE_WEB_USER)@$(RELEASE_WEB_HOST) ln -s $(VERSION) $(RELEASE_WEB_PATH)/LATEST-IS-$(VERSION)

tag-release:
	TRUNK_PATH="$$(svn info | grep '^URL: ' | cut -f2 -d' ')"; \
	TAG_PATH="$${TRUNK_PATH%/trunk/*}/tags/$(PACKAGE)-$(VERSION)"; \
	svn cp "$${TRUNK_PATH}" "$${TAG_PATH}" -m "tag for $(VERSION)"

release: prepare-release upload-release tag-release
	rm -rf release-data
