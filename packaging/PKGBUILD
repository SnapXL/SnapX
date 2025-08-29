# Maintainer: Brycen Granville <brycengranville@outlook.com>
pkgbase=snapx
pkgname=(snapx snapx-ui)
pkgver=0.4.0
pkgrel=4
pkgdesc="Screenshot tool that handles images, text, and video (fork of ShareX)"
arch=('x86_64' 'aarch64')
url="https://github.com/SnapXL/SnapX"
license=('GPL-3.0-or-later')
makedepends=(
    'git'
    'dotnet-sdk>=9.0'
    'clang'
    'zlib'
)
source=("$pkgbase::git+https://github.com/SnapXL/SnapX.git")
sha256sums=('SKIP')

pkgver() {
    cd "$pkgbase"
    ver=$(./build.sh --version | tail -n1)
    ver="${ver//-/_}"
    ver=${ver%%_makepkg*}
    echo "${ver}"
}


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
        'fontconfig'
        'freetype2'
        'openssl'
        'icu'
        'at'
        'sudo'
        'libxrandr'
        'libxcb'
        'dbus'
        'xdg-utils'
    )

    cd "$pkgbase"
    ./build.sh install --prefix /usr --dest-dir "${pkgdir}" --assembly snapx --skip compile
}

package_snapx-ui() {
    pkgdesc="Screenshot tool that handles images, text, and video (fork of ShareX). SnapX Avalonia-based UI (works best on X11)"
    depends=('snapx')

    cd "$pkgbase"
    ./build.sh install --prefix /usr --dest-dir "${pkgdir}" --assembly snapx-ui --skip compile
}
