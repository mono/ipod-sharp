TEST_SOURCES =			\
	$(srcdir)/IPodTests.cs \
	$(srcdir)/HashTests.cs

TEST_ASSEMBLY = ipod-sharp-tests.dll

$(TEST_ASSEMBLY): $(TEST_SOURCES) $(top_builddir)/src/ipod-sharp.dll
	$(MCS) -debug -r:$(top_builddir)/src/ipod-sharp.dll   \
	-r:$(srcdir)/nunit.framework $(TEST_SOURCES) -target:library -out:$@

all: $(TEST_ASSEMBLY)

TEST_TARBALLS =					\
	$(srcdir)/ipod-test-db-blank.tar.gz	\
	$(srcdir)/ipod-test-db-v12-a.tar.gz	\
	$(srcdir)/ipod-test-db-rev3-a.tar.gz	\
	$(srcdir)/ipod-test-db-rev4-a.tar.gz	\
	$(srcdir)/ipod-test-db-nano-v14-a.tar.gz

ipod-sharp.dll:
	ln -s $(top_builddir)/src/ipod-sharp.dll .

check: $(TEST_ASSEMBLY) ipod-sharp.dll
	for asm in $(TEST_TARBALLS); do NUNIT_DIR=$(srcdir) $(srcdir)/runtests.sh $$asm; done;

CLEANFILES = $(TEST_ASSEMBLY) *.mdb
MAINTAINERCLEANFILES = Makefile.in

EXTRA_DIST = 			\
	$(TEST_SOURCES)		\
	$(TEST_TARBALLS)	\
	runtests.sh		\
	mktest.sh		\
	nunit.core.dll		\
	nunit.framework.dll	\
	nunit.util.dll		\
	nunit-console.exe	\
	nunit-console.exe.config

