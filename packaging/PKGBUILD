# Maintainer: Brycen Granville <brycengranville@outlook.com>
pkgbase=snapx
pkgname=(snapx snapx-ui)
pkgver=0.1.0
pkgrel=1
pkgdesc="Screenshot tool that handles images, text, and video (fork of ShareX)"
arch=('x86_64' 'aarch64')
url="https://github.com/BrycensRanch/SnapX"
license=('GPL-3.0-or-later')
makedepends=(
    'git'
    'dotnet-sdk>=9.0'
    'clang'
    'zlib'
    'curl'
)

source=("$pkgbase::git+https://github.com/BrycensRanch/SnapX.git")
sha256sums=('SKIP')

build() {
    cd "$pkgbase"
    export VERSION=$pkgver
    export PKGTYPE=ARCH
    ./build.sh --configuration Release
}

check() {
    cd "$pkgbase"
    Output/snapx/snapx --version
}

package_snapx() {
    depends=(
        'ffmpeg'
        'curl'
        'fontconfig'
        'freetype2'
        'openssl'
        'icu'
        'at'
        'sudo'
        'libxrandr'
        'libxcb'
        'dbus'
        'vlc'
        'xdg-utils'
    )

     cd "$pkgbase"
     ./build.sh install --prefix /usr --dest-dir "${pkgdir}" --skip compile
     rm -f "${pkgdir}/usr/bin/snapx-ui"
}

package_snapx-ui() {
    pkgdesc="Screenshot tool that handles images, text, and video (fork of ShareX). SnapX Avalonia-based UI (works best on X11)"
    depends=('snapx')

    cd "$pkgbase"
    ./build.sh install --prefix /usr --dest-dir "${pkgdir}" --skip compile

    # Keep the files only for Avalonia UI
    rm -rf "${pkgdir}/usr/share"
    rm -f "${pkgdir}/usr/bin/snapx"
}
