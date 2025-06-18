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


%global version         0.2.1
# This build switch is not intended to be used as a method to make s390x and Ppc64le work
%global build_with_aot  false
%ifarch x86_64 aarch64
%global build_with_aot true
%endif

# .NET is not supported by either of these.
%define _debugsource_template %{nil}
%global         debug_package %{nil}

Name:           snapx
Version:        %{version}
Release:        4%{?dist}
Summary:        Screenshot tool that handles images, text, and video.

License:        GPL-3.0-or-later
URL:            https://github.com/BrycensRanch/SnapX
Source:         %{url}/archive/refs/heads/develop.tar.gz

# RISCV64 support is coming soon. Maybe .NET 10 will add it?
ExclusiveArch:  x86_64 aarch64

BuildRequires:  dotnet-sdk-9.0

%if "%{build_with_aot}" == "true"
# When installing AOT support, also install all dependencies needed to build
# NativeAOT applications. AOT invokes `clang ... -lssl -lcrypto -lbrotlienc
# -lbrotlidec -lz ...`.
BuildRequires:  pkgconfig(libbrotlidec)
BuildRequires:  clang
BuildRequires:  openssl-devel
BuildRequires:  zlib-devel
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
Requires:       (vlc-libs or pkgconfig(libvlc) or vlc-devel)


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

%global build_extra_args %{nil}
%if "%{build_with_aot}" != "true"
%global build_extra_args --extra-args="-p:PublishAot=false"
%endif

./build.sh --no-color --no-extended-chars --configuration Release %{build_extra_args}

%install
./build.sh install --no-color --no-extended-chars --prefix %{_prefix} --lib-dir %{buildroot}%{_libdir} --dest-dir %{buildroot} --doc-dir %{buildroot}%{_docdir}/%{name} --skip compile

%files
%{_bindir}/%{name}
%{_libdir}/%{name}
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
