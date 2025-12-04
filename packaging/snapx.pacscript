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
    ver=$(./build.sh --version | tail -n1)
    ver="${ver//-/_}"
    ver=${ver%%_makepkg*}
    echo "${ver}"
}


build() {
    cd "$pkgbase"
    git fetch --unshallow || true
    export VERSION=$pkgver
    export PKGTYPE=DEB
    ./build.sh --configuration Release
}

check() {
    cd "$pkgbase"
    Output/snapx/snapx --version
}

package_snapx() {
    depends=(
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
    )
    pacdeps=("ffmpeg")
    cd "$pkgbase"
    ./build.sh install --prefix /usr --dest-dir "${pkgdir}" --assembly snapx --skip compile
}

package_snapx-ui() {
    pkgdesc="Screenshot tool that handles images, text, and video (fork of ShareX). SnapX Avalonia-based UI (works best on X11)"
    pacdeps=('snapx')

    cd "$pkgbase"
    ./build.sh install --prefix /usr --dest-dir "${pkgdir}" --assembly snapx-ui --skip compile
}
