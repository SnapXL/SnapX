#!/usr/bin/env sh

if [ -n "$BASH_VERSION" ]; then
    bash --version | head -n1
elif [ -n "$ZSH_VERSION" ]; then
    zsh --version
else
    if [ -n "$SHELL" ]; then
        shell=$(basename "$SHELL")
    else
        shell=$(ps -p $$ -o comm= 2>/dev/null | awk -F/ '{print $NF}')
    fi
    if command -v "$shell" >/dev/null 2>&1 && "$shell" --version >/dev/null 2>&1; then
        "$shell" --version | head -n1
    else
        echo "$shell (version unknown)"
    fi
fi

set -eu
if [ -n "${BASH_SOURCE+x}" ]; then
  # shellcheck disable=SC3054
  SCRIPT_PATH="${BASH_SOURCE[0]}"
else
  SCRIPT_PATH="$0"
fi
SCRIPT_DIR=$(cd "$(dirname "$SCRIPT_PATH")" && pwd)

os_name=$(uname -o 2>/dev/null || echo "unknown")

if [ "$os_name" = "Msys" ] || [ "$os_name" = "Cygwin" ]; then
    SCRIPT_DIR=$(cygpath "$SCRIPT_DIR")
fi

###########################################################################
# CONFIGURATION
###########################################################################

BUILD_PROJECT_FILE="$SCRIPT_DIR/build/build.csproj"
TEMP_DIRECTORY="$SCRIPT_DIR/build/temp"


DOTNET_INSTALL_URL="https://dot.net/v1/dotnet-install.sh"
DOTNET_CHANNEL="STS"

export AVALONIA_TELEMETRY_OPTOUT=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_NOLOGO=1

###########################################################################
# EXECUTION
###########################################################################


if [ "$os_name" = "Darwin" ]; then
    USER_MACOS_VERSION=$(sw_vers -productVersion)
    USER_MACOS_VERSION_INT=$(echo "$USER_MACOS_VERSION" | awk -F. '{ printf "%d%02d", $1, $2 }')

    REQUIRED_VERSION_INT=1203

    if [ "$USER_MACOS_VERSION_INT" -lt "$REQUIRED_VERSION_INT" ]; then
        echo "This build will most likely fail as you are running an old version of macOS. Continue anyway? (y/n)"
        read -r response
        if [ "$response" = "n" ]; then
            echo "Exiting..."
            exit 1
        fi
    fi
fi

# If dotnet CLI is installed globally and it matches requested version, use for execution
IS_CI="${CI:-}"
IS_COPR="${COPR_PROJECT:-}${COPR_USERNAME:-}${COPR_CHROOT:-}"

if [ -x "$(command -v dotnet)" ] && dotnet --version >/dev/null 2>&1; then
    DOTNET_EXE=$(command -v dotnet)
    export DOTNET_EXE
elif { [ "$IS_CI" = "true" ] || [ -n "$IS_COPR" ]; } && [ "${ALLOW_DOTNET_DOWNLOAD:-0}" != "1" ]; then
    echo "Error: CI or COPR builds are not allowed to download dotnet by default. Set ALLOW_DOTNET_DOWNLOAD=1 to override." >&2
    exit 1
else
    # Download install script
    DOTNET_INSTALL_FILE="$TEMP_DIRECTORY/dotnet-install.sh"
    mkdir -p "$TEMP_DIRECTORY"
    curl -Lsfo "$DOTNET_INSTALL_FILE" "$DOTNET_INSTALL_URL"
    chmod +x "$DOTNET_INSTALL_FILE"

    # Install by channel or version
    DOTNET_DIRECTORY="$TEMP_DIRECTORY/dotnet-unix"
    if [ -z "${DOTNET_VERSION+x}" ]; then
        "$DOTNET_INSTALL_FILE" --install-dir "$DOTNET_DIRECTORY" --channel "$DOTNET_CHANNEL" --no-path
    else
        "$DOTNET_INSTALL_FILE" --install-dir "$DOTNET_DIRECTORY" --version "$DOTNET_VERSION" --no-path
    fi
    export DOTNET_EXE="$DOTNET_DIRECTORY/dotnet"
    export PATH="$DOTNET_DIRECTORY:$PATH"
fi

echo "Microsoft (R) .NET SDK version $("$DOTNET_EXE" --version)"

"$DOTNET_EXE" build "$BUILD_PROJECT_FILE" -nodeReuse:false -p:UseSharedCompilation=false -nologo -clp:NoSummary --verbosity quiet
"$DOTNET_EXE" run --project "$BUILD_PROJECT_FILE" --no-build -- "$@"
