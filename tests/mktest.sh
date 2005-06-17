#!/bin/bash

# I suck at shell script, sorry.

if [ -x $2 ]; then \
    echo "Usage: mktest.sh <db_file> <output_tarball>"
    exit 0
fi;

rm -rf ipod-test-db
mkdir -p ipod-test-db/iPod_Control/iTunes &&
cp $1 ipod-test-db/iPod_Control/iTunes/iTunesDB &&
tar cvfz $2 ipod-test-db 2>&1 > /dev/null
