#
# spec file for package snapx, and snapx-ui
#
# Copyright (c) 2024-2025 Brycen Granville <brycengranville@outlook.com>
#
# All modifications and additions to the file contributed by third parties
# remain the property of their copyright owners, unless otherwise agreed
# upon. The license for this file, and modifications and additions to the
# file, is the same license as for the pristine package itself (unless the
# license for the pristine package is not an Open Source License, in which
# case the license is the MIT License). An "Open Source License" is a
# license that conforms to the Open Source Definition (Version 1.9)
# published by the Open Source Initiative.

# Please submit bugfixes or comments via https://github.com/BrycensRanch/SnapX/issues


# This spec requires internet access! This is only meant to be built on Fedora COPR at the moment!


%global version         0.4.0
%ifarch x86_64 aarch64
    %{!?build_with_aot:%global build_with_aot true}
%else
    %{!?build_with_aot:%global build_with_aot false}
%endif

# .NET is not supported by either of these.
%define _debugsource_template %{nil}
%global         debug_package %{nil}

Name:           snapx
Version:        %{version}
%if 0%{?fedora}
Release:        %autorelease
%else
Release:        2%{?dist}
%endif
Summary:        Screenshot tool that handles images, text, and video.

License:        GPL-3.0-or-later
URL:            https://github.com/BrycensRanch/SnapX
Source:         %{url}/archive/refs/heads/develop.tar.gz

# RISCV64 support is coming soon. Maybe .NET 10 will add it?
ExclusiveArch:  x86_64 aarch64 ppc64le s390x

BuildRequires:  dotnet-sdk-9.0
BuildRequires:  (patchelf or chrpath)

%if "%{build_with_aot}" == "true"
# When installing AOT support, also install all dependencies needed to build
# NativeAOT applications. AOT invokes `clang ... -lssl -lcrypto -lbrotlienc
# -lbrotlidec -lz ...`.
BuildRequires:  pkgconfig(libbrotlidec)
BuildRequires:  clang
BuildRequires:  openssl-devel
BuildRequires:  zlib-devel
%endif

%if "%{build_with_aot}" != "true"
Requires:       dotnet-runtime-9.0
%endif

Recommends:     /usr/bin/ffmpeg
# Generic Avalonia Dependencies
Requires:       fontconfig, freetype, openssl, glibc, libicu, at, sudo, libXrandr, libxcb, dbus
# Required for opening browser tabs across Linux desktops
Requires:       xdg-utils

%description
This is a port of the original ShareX application to Linux.
It is not an official release and is not affiliated with the original ShareX project.
Specifically, it is the CLI tool.

%package ui
Summary:        SnapX Avalonia-based UI
Requires:       snapx


%description ui
This is a port of the original ShareX application to Linux.
It is not an official release and is not affiliated with the original ShareX project.
SnapX but with Avalonia. Works best on X11.

%prep
%autosetup -n SnapX-develop

%build

# Setup the correct compilation flags for the environment
# Not all distributions do this automatically
%if 0%{?fedora}
    # Do nothing, since Fedora 33 the build flags are already set
%else
    %set_build_flags
%endif
export PATH=$PATH:/usr/local/bin
export VERSION=%{version}
export PKGTYPE=RPM

%if "%{build_with_aot}" != "true"
    %{!?build_extra_args:%global build_extra_args --extra-args="-p:PublishAot=false -p:PublishSingleFile=false -p:PublishReadyToRun=false -p:SelfContained=false -p:PublishTrimmed=false -p:Optimize=false --use-current-runtime"}
%else
    %{!?build_extra_args:%global build_extra_args %{nil}}
%endif

./build.sh --no-color --no-extended-chars --configuration Release %{build_extra_args}

%install
export ELEVATION_NOT_NEEDED=1
./build.sh install --no-color --no-extended-chars --prefix %{_prefix} --dest-dir %{buildroot} --doc-dir %{buildroot}%{_docdir}/%{name} --skip compile

if command -v patchelf >/dev/null 2>&1; then
    PATCH_CMD="patchelf"
elif command -v chrpath >/dev/null 2>&1; then
    PATCH_CMD="chrpath"
else
    echo "ERROR: Neither patchelf nor chrpath found! Cannot patch RPATH."
    exit 1
fi

# Bandaid fix until upstream addresses these issues.
#ERROR   0002: file '/usr/lib/snapx/libphi.so' contains an invalid runpath '/home/runner/work/PaddleSharp/PaddleSharp/paddle-src/build/paddle/phi' in [/home/runner/work/PaddleSharp/PaddleSharp/paddle-src/build/paddle/phi:/home/runner/work/PaddleSharp/PaddleSharp/paddle-src/build/paddle/common]
#ERROR   0002: file '/usr/lib/snapx/libphi.so' contains an invalid runpath '/home/runner/work/PaddleSharp/PaddleSharp/paddle-src/build/paddle/common' in [/home/runner/work/PaddleSharp/PaddleSharp/paddle-src/build/paddle/phi:/home/runner/work/PaddleSharp/PaddleSharp/paddle-src/build/paddle/common]
#ERROR   0002: file '/usr/lib/snapx/libphi_core.so' contains an invalid runpath '/home/runner/work/PaddleSharp/PaddleSharp/paddle-src/build/paddle/common' in [/home/runner/work/PaddleSharp/PaddleSharp/paddle-src/build/paddle/common]
for f in %{buildroot}%{_prefix}/lib/%{name}/*.so; do
    echo "Patching $f ..."
    if [ "$PATCH_CMD" = "patchelf" ]; then
        patchelf --set-rpath '$ORIGIN' "$f"
    else
        chrpath -r '$ORIGIN' "$f" || true # Not every *.so has a rpath
    fi
done

%files
%{_bindir}/%{name}
%{_prefix}/lib/%{name}
%{_datadir}/SnapX
%{_docdir}/%{name}
%license LICENSE.md

%files ui
%{_bindir}/%{name}-ui
%{_datadir}/applications/io.github.BrycensRanch.SnapX.desktop
%{_datadir}/metainfo/io.github.BrycensRanch.SnapX.metainfo.xml
%{_datadir}/icons/hicolor/48x48/apps/io.github.BrycensRanch.SnapX.png
%{_datadir}/icons/hicolor/128x128/apps/io.github.BrycensRanch.SnapX.png
%{_datadir}/icons/hicolor/256x256/apps/io.github.BrycensRanch.SnapX.png
%{_datadir}/icons/hicolor/scalable/apps/io.github.BrycensRanch.SnapX.svg
%license LICENSE.md


%if 0%{?fedora}
%changelog
%autochangelog
%else


%changelog
* Mon Nov 18 2024 Brycen G <brycengranville@outlook.com> - 0.0.0-1
- Initial package
%endif
