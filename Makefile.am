SUBDIRS = src ui tools

if BUILD_TESTS
SUBDIRS += tests
endif

pkgconfigdir = $(libdir)/pkgconfig
pkgconfig_DATA = ipod-sharp.pc ipod-sharp-ui.pc

EXTRA_DIST = ipod-sharp.snk

release: dist
	svn cp svn+ssh://jwilcox@mono-cvs.ximian.com/source/trunk/ipod-sharp \
	svn+ssh://jwilcox@mono-cvs.ximian.com/source/tags/ipod-sharp-$(VERSION) -m "tag for $(VERSION)"
	scp ipod-sharp-$(VERSION).tar.gz root@snorp.net:/var/www/html/files/ipod-sharp
