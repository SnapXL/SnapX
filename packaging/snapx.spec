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

# Please submit bugfixes or comments via https://github.com/SnapXL/SnapX/issues


# This spec requires internet access! This is only meant to be built on GitHub Actions at the moment!
%global base_release 3
%global full_version %(../build.sh --version | tail -n1 | tr -d '\n' || echo 0.4.0)

# extract upstream version (everything before the last dot-number+git)
%global version %(echo "%{full_version}" | sed 's/\.[^.]*$//; s/-/~/g')

# extract commit number and git sha
%global gitversion %(echo "%{full_version}" | awk -F. '{print $NF}' | tr '+' '.')

%ifarch x86_64 aarch64 armhf armv7hl armv7l
    %{!?build_with_aot:%global build_with_aot true}
%else
    %{!?build_with_aot:%global build_with_aot false}
%endif

%define github_path %{?github_repo}%{!?github_repo:SnapXL/SnapX}

%define repo_name   %(basename %{github_path})

%define git_ref     %{?github_sha}%{!?github_sha:develop}
%define clean_ref   %(echo %{git_ref} | sed 's/^v//')

# .NET is not supported by either of these.
%define _debugsource_template %{nil}
%global         debug_package %{nil}

Name:           snapx
Version:        %{version}
Release:        %{base_release}.%{?gitversion}%{?dist}
Summary:        Screenshot tool that handles images, text, and video.
Packager:       Brycen G <brycengranville@outlook.com>

License:        GPL-3.0-or-later
URL:            https://github.com/%{github_path}
Source:         %{url}/archive/%{git_ref}.tar.gz

# RISCV64 support is coming soon. Maybe .NET 10 will add it?
ExclusiveArch:  x86_64 aarch64 ppc64le s390x armhf armv7hl armv7l

BuildRequires:  dotnet-sdk-10.0

%if "%{build_with_aot}" == "true"
# When installing AOT support, also install all dependencies needed to build
# NativeAOT applications. AOT invokes `clang ... -lssl -lcrypto -lbrotlienc
# -lbrotlidec -lz ...`.
BuildRequires:  pkgconfig(libbrotlidec)
BuildRequires:  clang
BuildRequires:  openssl-devel
BuildRequires:  zlib-devel
%endif

Requires:       snapx-core = %{version}-%{release}

%description
This is a port of the original ShareX application to Linux.
It is not an official release and is not affiliated with the original ShareX project.
Specifically, it is the CLI tool.

%package core
Summary:        Shared libraries and core logic for SnapX

%if "%{build_with_aot}" != "true"
Requires:       dotnet-runtime-10.0
%endif

Recommends:     /usr/bin/ffmpeg
Recommends:     /usr/bin/glxinfo
Recommends:     /usr/bin/lspci
Recommends:     /usr/bin/xrandr

# Required for opening browser tabs across Linux desktops
Requires:       xdg-utils
Requires:       /usr/bin/avifenc

%description core
This package contains the heavy dependencies and shared libraries used by both
the CLI and the UI.

%package ui
Summary:        SnapX Avalonia-based UI
Requires:       snapx-core = %{version}-%{release}
# libicu was removed because we now compile with InvariantGlobalization
Requires:       fontconfig, freetype, openssl, glibc, at, sudo, libXrandr, libxcb, dbus

%description ui
This is a port of the original ShareX application to Linux.
It is not an official release and is not affiliated with the original ShareX project.
SnapX but with Avalonia. Works best on X11.

%prep
%autosetup -n %{repo_name}-%{clean_ref}

%build
# Setup the correct compilation flags for the environment
# Not all distributions do this automatically
%if 0%{?fedora}
    # Do nothing, since Fedora 33 the build flags are already set
%else
    %set_build_flags
%endif
export PATH=$PATH:/usr/local/bin
export PKGTYPE=RPM

%if "%{build_with_aot}" != "true"
    %{!?build_extra_args:%global build_extra_args --extra-args="-p:PublishAot=false -p:PublishSingleFile=false -p:PublishReadyToRun=false -p:SelfContained=false -p:PublishTrimmed=false -p:Optimize=false --use-current-runtime"}
%else
    %{!?build_extra_args:%global build_extra_args %{nil}}
%endif

set +x  # Prevent secrets leaking.
if [ -n "${API_KEYS:-}" ]; then
    curl -fsSL "$API_KEYS" -o SnapX.Core/Upload/APIKeysLocal.cs
    echo "🔐 API Keys have been DEPLOYED into this build."
else
    echo "⚠️ No API_KEYS environment variable defined, skipping API key download."
fi
set -x

./build.sh --no-color --no-extended-chars --configuration Release %{build_extra_args}

%install
export ELEVATION_NOT_NEEDED=1
./build.sh install --no-color --no-extended-chars --prefix %{_prefix} --dest-dir %{buildroot} --doc-dir %{buildroot}%{_docdir}/%{name} --skip compile

%check
Output/snapx-ui/snapx-ui --version


# This message will only show on first install, not upgrades/removals
# Additionally, it will be removed once SnapX's CLI is mature.
%post
if [ "$1" -eq 1 ]; then
    echo "--------------------------------------------------------------"
    echo "  Welcome to SnapX! Thank you so much for testing the app.    "
    echo "--------------------------------------------------------------"
    echo "  NOTE: The 'snapx' CLI is still under active development.    "
    echo "  For the best experience right now, please use 'snapx-ui'.   "
    echo ""
    echo "  If you enjoy the project, please consider:                  "
    echo "  ⭐ Starring on GitHub: https://github.com/%{github_path}"
    echo "  🚲 Donating to help me fund my bus fare and keep my scooter charged!"
    echo "     https://liberapay.com/BrycensRanch"
    echo "--------------------------------------------------------------"
fi

%files core
%{_prefix}/lib/%{name}
%{_datadir}/SnapX
%{_docdir}/%{name}
%exclude %{_prefix}/lib/%{name}/%{name}
%exclude %{_prefix}/lib/%{name}/avif*
%exclude %{_prefix}/lib/%{name}/%{name}-ui
%exclude %{_prefix}/lib/%{name}/libHarfBuzzSharp.so
%exclude %{_prefix}/lib/%{name}/libSkiaSharp.so
%license LICENSE.md

%files
%{_prefix}/lib/%{name}/%{name}
%{_bindir}/%{name}
%license LICENSE.md

%files ui
%{_prefix}/lib/%{name}/%{name}-ui
%{_bindir}/%{name}-ui
%{_prefix}/lib/%{name}/libHarfBuzzSharp.so
%{_prefix}/lib/%{name}/libSkiaSharp.so
%{_datadir}/applications/io.github.SnapXL.SnapX.desktop
%{_datadir}/metainfo/io.github.SnapXL.SnapX.metainfo.xml
%{_datadir}/icons/hicolor/48x48/apps/io.github.SnapXL.SnapX.png
%{_datadir}/icons/hicolor/128x128/apps/io.github.SnapXL.SnapX.png
%{_datadir}/icons/hicolor/256x256/apps/io.github.SnapXL.SnapX.png
%{_datadir}/icons/hicolor/scalable/apps/io.github.SnapXL.SnapX.svg
%license LICENSE.md


%if 0%{?fedora}
%changelog
%autochangelog
%else


%changelog
* Mon Nov 18 2024 Brycen G <brycengranville@outlook.com> - 0.0.0-1
- Initial package
%endif
