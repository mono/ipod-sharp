TEST_SOURCES =			\
	$(srcdir)/IPodTests.cs

TEST_ASSEMBLY = ipod-sharp-tests.dll

$(TEST_ASSEMBLY): $(TEST_SOURCES) $(top_builddir)/src/ipod-sharp.dll
	$(MCS) -debug -r:$(top_builddir)/src/ipod-sharp.dll   \
	-r:$(srcdir)/nunit.framework $(TEST_SOURCES) -target:library -o $@

all: $(TEST_ASSEMBLY)

TEST_TARBALLS =					\
	$(srcdir)/ipod-test-db-v12-a.tar.gz	\
	$(srcdir)/ipod-test-db-rev2-a.tar.gz	\
	$(srcdir)/ipod-test-db-rev3-a.tar.gz	\
	$(srcdir)/ipod-test-db-rev4-a.tar.gz	\
	$(srcdir)/ipod-test-db-shuffle-a.tar.gz

check: $(TEST_ASSEMBLY)
	for asm in $(TEST_TARBALLS); do NUNIT_DIR=$(srcdir) $(srcdir)/runtests.sh $$asm; done;

CLEANFILES = $(TEST_ASSEMBLY)

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

