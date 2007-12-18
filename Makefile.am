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

release-tag:
	@-test ! -z $$MONO_USER || { echo "Must set MONO_USER env."; exit 1; }; \
	svn cp svn+ssh://$$MONO_USER@mono-cvs.ximian.com/source/trunk/ipod-sharp \
		svn+ssh://$$MONO_USER@mono-cvs.ximian.com/source/tags/ipod-sharp-$(VERSION) -m "tag for $(VERSION)"

snorp-upload: distcheck
	scp ipod-sharp-$(VERSION).tar.gz root@snorp.net:/var/www/files/ipod-sharp

abock-upload: distcheck
	scp ChangeLog ipod-sharp-$(VERSION).tar.gz banshee@banshee-project.org:public_html/files/ipod-sharp

