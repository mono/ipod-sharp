TEST_SOURCES =			\
	$(srcdir)/IPodTests.cs

TEST_ASSEMBLY = ipod-sharp-tests.dll

$(TEST_ASSEMBLY): $(TEST_SOURCES)
	$(MCS) -debug -r:$(top_builddir)/src/ipod-sharp.dll   \
	-r:$(srcdir)/nunit.framework $(TEST_SOURCES) -target:library -o $@

all: $(TEST_ASSEMBLY)

check: $(TEST_ASSEMBLY)
	IPOD_SHARP_TEST_TARBALL=$(srcdir)/ipod-test-db-v12-a.tar.gz mono --debug $(srcdir)/nunit-console.exe /nologo $(TEST_ASSEMBLY)
	IPOD_SHARP_TEST_TARBALL=$(srcdir)/ipod-test-db-rev2-a.tar.gz mono --debug $(srcdir)/nunit-console.exe /nologo $(TEST_ASSEMBLY)
	IPOD_SHARP_TEST_TARBALL=$(srcdir)/ipod-test-db-rev3-a.tar.gz mono --debug $(srcdir)/nunit-console.exe /nologo $(TEST_ASSEMBLY)
	IPOD_SHARP_TEST_TARBALL=$(srcdir)/ipod-test-db-rev4-a.tar.gz mono --debug $(srcdir)/nunit-console.exe /nologo $(TEST_ASSEMBLY)
	IPOD_SHARP_TEST_TARBALL=$(srcdir)/ipod-test-db-shuffle-a.tar.gz mono --debug $(srcdir)/nunit-console.exe /nologo $(TEST_ASSEMBLY)

CLEANFILES = $(TEST_ASSEMBLY)

EXTRA_DIST = 			\
	$(TEST_SOURCES)		\
	ipod-test-db-v12-a.tar.gz	\
	nunit.core.dll		\
	nunit.framework.dll	\
	nunit.util.dll		\
	nunit-console.exe	\
	nunit-console.exe.config

