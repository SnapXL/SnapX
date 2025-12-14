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

# if command -v timeout >/dev/null 2>&1; then
#     timeout 5s sh -c "LD_DEBUG=libs \"$EXE\" >/dev/null 2>\"$TMP\""
# else
    LD_DEBUG=libs "$EXE" >/dev/null 2>"$TMP" &
    PID=$!
    sleep 5
    kill "$PID" 2>/dev/null || true
# fi

echo "Last 20 lines of output:"
tail -n 20 "$TMP"

LIBS=$(awk '/calling init:/ {
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
lspci
dmidecode
grep
bash
"

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

rm -f "$BLACKLIST_FILE"
SCRIPT_DIR="$(cd -- "$(dirname -- "$0")" && pwd -P)"

INTERPRETER=""
if [ -x "$(command -v patchelf)" ]; then
    for f in "$DEST"/ld*.so*; do
        [ -e "$f" ] || continue
        INTERPRETER="$(basename "$f")"
        break
    done
    if [ -z "$INTERPRETER" ]; then
        echo "ERROR: No dynamic linker found in $DEST"
        exit 1
    fi
fi

set -x

for lib in $LIBS; do
    if [ -n "$lib" ] && [ -f "$lib" ]; then
        echo "Copying library: $lib"
        $SCRIPT_DIR/copy_deps.sh "$lib" "$DEST/"
        if [ -x "$(command -v patchelf)" ]; then
            patchelf --force-rpath --set-rpath "\$ORIGIN" "$DEST/$(basename "$lib")" || echo "WARNING: Could not patchelf $DEST/$(basename "$lib")"
        fi
    fi
done

for program in $PROGRAMS; do
    [ -n "$program" ] || continue
    full_path=$(command -v -- "$program" 2>/dev/null) || continue

    if [ -f "$full_path" ]; then
        echo "Copying program: $full_path"
        $SCRIPT_DIR/copy_deps.sh "$full_path" "$DEST/"
        cp -L "$full_path" "$DEST/"
        if [ -x "$(command -v patchelf)" ]; then
            patchelf --set-interpreter "$(basename "$DEST")/$INTERPRETER" --force-rpath --set-rpath "\$ORIGIN" "$DEST/$program" || echo "WARNING: Could not patchelf $program"
        fi
    fi
done

