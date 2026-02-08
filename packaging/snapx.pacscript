# Maintainer: Brycen Granville <brycengranville@outlook.com>
pkgbase=snapx-core
pkgname=(snapx-core snapx snapx-ui)
pkgver=0.4.0~alpha.r863.g133c8dc
pkgrel=1
pkgdesc="Screenshot tool that handles images, text, and video (fork of ShareX)"
arch=('x86_64' 'aarch64' 'armv7h')
url="https://github.com/SnapXL/SnapX"
license=('GPL-3.0-or-later')
makedepends=(
    'git'
    'clang'
    'zlib1g'
)
source=("$pkgbase::git+https://github.com/SnapXL/SnapX.git#branch=develop")
sha256sums=('SKIP')
maintainer=("Brycen Granville <brycengranville@outlook.com>")
external_connection=true

pkgver() {
    cd "$pkgbase"
    git fetch --unshallow || true
    git checkout -B develop >/dev/null 2>&1
    ver=$(./build.sh --version | tail -n1 | sed -E 's/^(.*)-(.*)\.([0-9]+)\+(.*)$/\1~\2.r\3.\4/')
    echo "$ver"
}


build() {
    cd "$pkgbase"
    git fetch --unshallow || true
    if [ -n "$API_KEYS" ]; then
        curl -fsSL "$API_KEYS" -o SnapX.Core/Upload/APIKeysLocal.cs
    fi
    git checkout -B develop >/dev/null 2>&1

    export VERSION=$pkgver
    export PKGTYPE=DEB
    # Build logic requires being on a known branch
    git checkout -B develop
    ./build.sh --configuration Release
    # We are going to use the system provided AVIF for now on
    find Output -type f -name 'avif*' -delete
}

check() {
    cd "$pkgbase"
    Output/snapx/snapx --version
}

package_snapx-core() {
    depends=(
        'ffmpeg'
        'fontconfig'
        'libfreetype6-dev'
        'openssl'
        'libicu74'
        'at'
        'sudo'
        'libxrandr2'
        'libxcb1'
        'dbus'
        'xdg-utils'
        'libavif-bin'
    )
    optdepends=(
        'mesa-utils: Graphics hardware information'
        'x11-xserver-utils: Display configuration diagnostics'
    )

    cd "$pkgbase"
    ./build.sh install --prefix /usr --dest-dir "${pkgdir}" --assembly snapx --skip compile
    rm -f "${pkgdir}/usr/lib/snapx/snapx"
    rm -f "${pkgdir}/usr/bin/snapx"
}
package_snapx() {
    pkgdesc="CLI-only interface for SnapX, a feature-rich screenshot and screencast tool (ShareX fork)"
    pacdeps=('snapx-core')

    local _temp_dir="${srcdir}/temp_install"
    mkdir -p "$_temp_dir"

    cd "$pkgbase"
    ./build.sh install --prefix /usr --dest-dir "$_temp_dir" --assembly snapx --skip compile

    install -d "${pkgdir}/usr/lib/snapx"
    install -d "${pkgdir}/usr/bin"

    cp -a "$_temp_dir/usr/lib/snapx/snapx" "${pkgdir}/usr/lib/snapx/"

    if [ -f "$_temp_dir/usr/bin/snapx" ]; then
        install -m 755 "$_temp_dir/usr/bin/snapx" "${pkgdir}/usr/bin/snapx"
    fi
}
package_snapx-ui() {
    pkgdesc="Screenshot tool that handles images, text, and video (fork of ShareX). SnapX Avalonia-based UI (works best on X11)"
    pacdeps=('snapx-core')

    cd "$pkgbase"
    ./build.sh install --prefix /usr --dest-dir "${pkgdir}" --assembly snapx-ui --skip compile
}
