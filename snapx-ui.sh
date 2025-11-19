#!/bin/sh

dir="$(cd -P -- "$(dirname -- "$0")" && pwd -P)"
cd "$dir"
export LD_LIBRARY_PATH="./libs:$LD_LIBRARY_PATH"
export LD_PRELOAD="./libs/libc.so.6"

if [ -e ./libs/ld*.so* ]; then
    exec -a ./libs/ld*.so* "./packaging/tarball/lib/$(basename "$0" .sh)" "$@"
else
    exec "./packaging/tarball/lib/$(basename "$0" .sh)" "$@"
fi
