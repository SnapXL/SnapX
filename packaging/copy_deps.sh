#!/usr/bin/env sh

set -eu

: "${PROCESSED_DEPS_FILE:=/tmp/processed_deps.lockfile}"

# Usage: copy_deps.sh <binary> <destination_dir>

copy_deps() {
    bin="$1"
    dest="$2"
    mkdir -p "$dest"
    destfile="$dest/$(basename "$bin")"

    case "$bin" in
        *.so* )
            if ! cmp -s "$bin" "$destfile"; then
                cp -L --no-preserve=mode "$bin" "$dest" || {
                    echo "Failed to copy $bin"
                    exit 1
                }
            fi
            ;;
    esac

    if ldd "$bin" 2>&1 | grep -q "statically linked"; then
        echo "✨ $bin is statically linked — no deps to copy."
        return 0
    fi
    flock "$PROCESSED_DEPS_FILE" grep -qxF "$bin" "$PROCESSED_DEPS_FILE" && return
    flock "$PROCESSED_DEPS_FILE" sh -c "echo '$bin' >> '$PROCESSED_DEPS_FILE'"
    cp -L --no-preserve=mode $(ldd "$bin" | grep -E '(^|[^a-zA-Z0-9])ld' | awk '{print $1}') "$dest" || {
        echo "Failed to copy dynamic linker"
        exit 1
    }

    # Copy direct dependencies
#    ldd "$bin" | awk '{print $3}' | grep -v 'not found' | while read dep; do
#        if [ -n "$dep" ] && [ -f "$dep" ]; then
#            destfile="$dest/$(basename "$dep")"
#            if ! cmp -s "$dep" "$destfile"; then
#                cp -L --no-preserve=mode "$dep" "$dest" || {
#                    echo "Failed to copy $dep"
#                    exit 1
#                }
#            fi
#
#            # Check if dependency itself is static
#            if ldd "$dep" 2>&1 | grep -q "statically linked"; then
#                echo "⚡ $dep is statically linked — skipping its deps."
#                continue
#            fi
#
#            # Copy subdependencies of each dep
#            ldd "$dep" | awk '{print $3}' | grep -v 'not found' | while read subdep; do
#                if [ -n "$subdep" ] && [ -f "$subdep" ]; then
#                    destfile="$dest/$(basename "$subdep")"
#                    if ! cmp -s "$subdep" "$destfile"; then
#                        cp -L --no-preserve=mode "$subdep" "$dest" || {
#                            echo "Failed to copy $subdep"
#                            exit 1
#                        }
#                    fi
#                fi
#            done
#        fi
#    done

    chmod +x "$dest"/*.so* 2>/dev/null || echo "Failed to set libraries executable! Oh well"
}

# Pass args through if script is run directly
if [ $# -eq 2 ]; then
    copy_deps "$1" "$2"
fi
