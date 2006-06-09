SUBDIRS = src ui tools

if BUILD_TESTS
SUBDIRS += tests
endif

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


release: dist
	svn cp svn+ssh://jwilcox@mono-cvs.ximian.com/source/trunk/ipod-sharp \
	svn+ssh://jwilcox@mono-cvs.ximian.com/source/tags/ipod-sharp-$(VERSION) -m "tag for $(VERSION)"
	scp ipod-sharp-$(VERSION).tar.gz root@snorp.net:/var/www/files/ipod-sharp
