#!/bin/bash

if test -z $1; then
    echo "Usage: runtests.sh <device_or_tarball>"
    exit 1
fi;

if test -d $1; then
    ./mktest.sh $1 tmp-test.tar.gz
    IPOD_SHARP_TEST_TARBALL="tmp-test.tar.gz"
elif test -f $1; then
    IPOD_SHARP_TEST_TARBALL=$1
else
    echo "Argument is not a device or tarball"
    exit 1
fi;

export IPOD_SHARP_TEST_TARBALL

exec mono --debug nunit-console.exe /nologo ipod-sharp-tests.dll
