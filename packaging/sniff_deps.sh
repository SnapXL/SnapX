#!/usr/bin/env sh

set -eu

if [ $# -ne 2 ]; then
    echo "Usage: $0 <executable> <destination-dir>"
    exit 1
fi

EXE="$1"
DEST="$2"

mkdir -p "$DEST"

TMP=$(mktemp) || exit 1

cleanup() {
    rm -f "$TMP"
}
trap cleanup EXIT

timeout 15s xvfb-run -w 5 sh -c '
env LD_DEBUG=libs SNAPX_SHOWTRAY=false "$1" >/dev/null 2>"$2"
' sh "$EXE" "$TMP" || true

echo "Last 20 lines of output:"
tail -n 20 "$TMP"

LIBS_EXTRACTED=$(awk '/calling init:/ {
    match($0,/calling init: ([^ ]+)/,a)
    if(a[1]) print a[1]
}' "$TMP" | sort -u)
PROGRAMS_EXTRACTED=$(
    awk '
        /initialize program:/ {
            match($0, /initialize program: ([^ ]+)/, b)
            # Only accept programs without a slash
            if (b[1] && index(b[1], "/") == 0) print b[1]
        }
    ' "$TMP" | sort -u
)
PROGRAMS_KNOWN="
ffmpeg
"

LIBRARIES_KNOWN="
libmsquic
"

# libLLVM is large
LIB_BLACKLIST="libLLVM.so|ld*.so*"

# Coreutils will be provided by cool Rust implementation
# Bash will be replaced with BRUSH! (Rust compatible bash shell)
PROGRAMS_BLACKLIST="
systemctl
sysctl
journalctl
login
logind
systemd
systemd-run
systemd-ask-password
systemd-inhibit
systemd-notify
systemd-tmpfiles
"

BLACKLIST_FILE=$(mktemp)
printf '%s\n' "$PROGRAMS_BLACKLIST" > "$BLACKLIST_FILE"

PROGRAMS=$(
    {
        printf '%s\n' $PROGRAMS_EXTRACTED
        printf '%s\n' $PROGRAMS_KNOWN
    } | grep -v -F -x -f "$BLACKLIST_FILE" | sort -u
)

resolve_lib() {
    name="$1"

    if command -v ldconfig >/dev/null 2>&1; then
        ldconfig -p 2>/dev/null \
        | awk -v n="$name" '$1 ~ "^"n"\\.so" {print $NF; exit}'
    else
        find /lib /usr/lib /lib64 /usr/lib64 2>/dev/null \
            -type f -name "$name.so*" | head -n 1
    fi
}

RAW_LIBS=$(
    {
        printf '%s\n' "$LIBS_EXTRACTED"

        for lib in $LIBRARIES_KNOWN; do
            resolved="$(resolve_lib "$lib")"
            [ -n "$resolved" ] && printf '%s\n' "$resolved"
        done
    } | sort -r | grep -vE "$LIB_BLACKLIST"
)

seen=""
final_list=""

for lib in $RAW_LIBS; do
    base=$(echo "$lib" | sed 's/\.so.*/.so/')

    case "$seen" in
        *" $base "*)
            # Already seen this library family, skip it
            ;;
        *)
            # New library family, add to final output
            final_list=$(printf '%s\n%s' "$final_list" "$lib")
            seen="$seen $base "
            ;;
    esac
done

LIBS=$(echo "$final_list" | sed '/^$/d')

rm -f "$BLACKLIST_FILE"
SCRIPT_DIR="$(cd -- "$(dirname -- "$0")" && pwd -P)"

INTERPRETER=""
#if [ -x "$(command -v patchelf)" ]; then
#    for f in "$DEST"/ld*.so*; do
#        [ -e "$f" ] || continue
#        INTERPRETER="$(basename "$f")"
#        break
#    done
#    if [ -z "$INTERPRETER" ]; then
#        echo "WARNING: No dynamic linker found (This is normal on FreeBSD, the dynamic linker is tied to the kernel) in $DEST"
#    fi
#fi
#
#for lib in $LIBS; do
#    if [ -n "$lib" ] && [ -f "$lib" ]; then
#        echo "Copying library: $lib"
#        $SCRIPT_DIR/copy_deps.sh "$lib" "$DEST/"
#        if [ -x "$(command -v patchelf)" ]; then
#            patchelf --force-rpath --set-rpath "\$ORIGIN" "$DEST/$(basename "$lib")" || echo "WARNING: Could not patchelf $DEST/$(basename "$lib")"
#        fi
#    fi
#done

for program in $PROGRAMS; do
    [ -n "$program" ] || continue
    full_path=$(command -v -- "$program" 2>/dev/null) || continue

    if [ -f "$full_path" ]; then
        echo "Copying program: $full_path"
        $SCRIPT_DIR/copy_deps.sh "$full_path" "$DEST/" || echo "Failed to copy deps of $program, PLEASE TEST BINARY TO ENSURE IT STILL WORKS"
        cp -L "$full_path" "$DEST/"
        if command -v patchelf >/dev/null 2>&1; then
            if [ -n "$INTERPRETER" ]; then
                patchelf --set-interpreter "$DEST/$INTERPRETER" --force-rpath --set-rpath '\$ORIGIN' "$DEST/$program" || echo "WARNING: Could not patchelf $program"
            else
                patchelf --force-rpath --set-rpath '\$ORIGIN' "$DEST/$program" || echo "WARNING: Could not patchelf $program"
            fi
        fi
    fi
done

