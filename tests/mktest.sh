#!/bin/bash

# I suck at shell script, sorry.

function usage()
{
    echo "Usage: mktest.sh <device> <output_tarball>"
    exit 1
}

if [ -z $2 ]; then \
    usage
elif [ ! -d $1 ]; then
    echo "Invalid device"
    usage
fi;

rm -rf ipod-test-db
mkdir -p ipod-test-db/iPod_Control &&
echo "Making test tarball '$2' from device '$1'" &&
cp -R $1/iPod_Control/iTunes ipod-test-db/iPod_Control &&
cp -R $1/iPod_Control/Device ipod-test-db/iPod_Control &&
tar cvfz $2 ipod-test-db 2>&1 > /dev/null &&
rm -rf ipod-test-db
