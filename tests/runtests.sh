
if test "x$1" = "x"; then
    echo "Usage: runtests.sh <test_tarball>"
    exit 1
fi;

IPOD_SHARP_TEST_TARBALL=$1 exec mono --debug nunit-console.exe /nologo ipod-sharp-tests.dll
