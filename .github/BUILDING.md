# Development Dependencies

- `git`
- `dotnet-sdk-10.0`
- `ffmpeg` (<7)
- `rust` & `cargo` (<1.80) (macOS only, the rest use SharpCapture)
- `clang`
- `zlib-devel`

#### IDE of Choice

JetBrains Rider is the recommended IDE. It works on Linux, Windows, and macOS. It's free for noncommercial use.

<a href="https://www.jetbrains.com/rider/" target="_blank">
  <img
    src="https://github.com/user-attachments/assets/96b8e44e-47b3-4850-b4f3-c4e7ed8dd385"
    alt="JetBrains Rider - The world's most loved .NET and game dev IDE"
    title="JetBrains Rider - The world's most loved .NET and game dev IDE"
    style="width: 400px; height: auto;"
  />
</a>

### Fedora 42+ <img src="https://upload.wikimedia.org/wikipedia/commons/4/41/Fedora_icon_%282021%29.svg" alt="Fedora Logo" height="25" width="25"/>

```bash
sudo dnf in -y git dotnet-sdk-aot-10.0 /usr/bin/ffmpeg
```

### Ubuntu 24.04+ <img src="https://upload.wikimedia.org/wikipedia/commons/9/94/Ubuntu_logoib.svg" alt="Ubuntu Logo" height="25" width="25"/>

```bash
sudo apt update && sudo apt install -y software-properties-common
sudo add-apt-repository ppa:dotnet/backports -y # Ubuntu 24.04 doesn't have .NET 10 packaged. Do not add this PPA on Ubuntu 24.10+
sudo add-apt-repository ppa:ubuntuhandbook1/ffmpeg8 -y # Ubuntu 24.04 doesn't have FFMPEG 7 packaged.
sudo apt install -y git dotnet-sdk-10.0 ffmpeg clang zlib1g-dev libsm6
```

### Windows 10 22H2+ <img src="https://upload.wikimedia.org/wikipedia/commons/5/5f/Windows_logo_-_2012.svg" alt="Windows Logo" height="20" width="20"/>

End of life Windows versions are not supported. For example, Windows 11 22H2 is at its EOL and, thus, unsupported.

SnapX now uses the Windows SDK to generate C# Windows API binding code.
You need the Windows 11 SDK `10.0.26100.0`.
It works on Windows 10, too.

```shell
# Installing Visual Studio Community
# You cannot build with NativeAOT without it on Windows. It has the linker program. However, you can compile on Rider or whatever your favorite IDE is after you've installed Visual Studio.
# See https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot
winget install --id Microsoft.VisualStudio.2022.Community --override "--quiet --add Microsoft.VisualStudio.Workload.NativeDesktop --add Microsoft.VisualStudio.Component.Windows11SDK.26100 --includeRecommended"
winget install -e --id Git.Git
# Install Rider (optional)
winget install -e --id JetBrains.Rider
```

### macOS 14 Sonoma+ <img src="https://upload.wikimedia.org/wikipedia/commons/1/1b/Apple_logo_grey.svg" alt="Apple Logo" height="25" width="20"/>

End of life macOS versions are not supported. For example, macOS 13 Ventura is at its EOL and thus, unsupported.

> Using the script to install the .NET SDK from the .NET team ensures you don't run into issues of Rider not detecting it.

```zsh
xcode-select --install
brew install ffmpeg@8 rust
git --version # If prompted to install Git, do it.
exec $SHELL -l
```

> [!TIP]
> If you're using MacPorts, run this instead of `brew install`:
>
> ```zsh
> sudo port selfupdate
> sudo port install ffmpeg8 cargo
> ```

### Alpine Linux 3.23+ <img src="https://github.com/user-attachments/assets/a44c5039-017c-4547-8598-8adafeae214b" alt="Alpine Linux Logo" height="25" width="25"/>


```bash
sudo apk update
# Add the edge repository (required for onnxruntime)
echo "http://dl-cdn.alpinelinux.org/alpine/edge/community" | sudo tee -a /etc/apk/repositories

# Only need to install dependencies of .NET, the build script installs the .NET version we're using automatically.
sudo apk add bash icu-libs krb5-libs libgcc libintl libssl3 libstdc++ zlib onnxruntime
# !!!IMPORTANT!!!
# Alpine's onnxruntime package doesn't provide libonnxruntime.so, only versioned files
sudo ln -sf /usr/lib/libonnxruntime.so.1 /usr/lib/libonnxruntime.so
```

### FreeBSD 14+ <img src="https://github.com/user-attachments/assets/3909625f-91be-4abc-bde6-e222921119ad" alt="FreeBSD Logo" height="25" width="25"/>


```bash
# Install required system dependencies
sudo pkg install -y curl bash icu terminfo-db libunwind libinotify \
    brotli openssl lttng-ust krb5 llvm21 sqlite3 sudo patchelf git \
    flock xorg xorg-vfbserver dbus glx-utils pciutils dmidecode \
    cmake rust-coreutils gawk onnxruntime

sudo mkdir -p /usr/lib/dotnet

# Download the community-built .NET 10 SDK for FreeBSD
curl -L -o /tmp/dotnet.tar.gz https://github.com/Thefrank/dotnet-freebsd-crossbuild/releases/download/v10.0.102-sb1-amd64-freebsd-14/dotnet-sdk-10.0.102-freebsd-x64.tar.gz

# Extract the SDK
sudo tar -xzf /tmp/dotnet.tar.gz -C /usr/lib/dotnet --strip-components=1
rm /tmp/dotnet.tar.gz

sudo ln -sf /usr/lib/dotnet/dotnet /usr/local/bin/dotnet
sudo ln -sf /usr/lib/dotnet/dnx /usr/local/bin/dnx 2>/dev/null || true
sudo find /usr/lib/dotnet/packs \( -name "ilc" -o -name "crossgen2" \) -exec chmod +x {} +

# Add the FreeBSD-specific NuGet source for runtime compatibility
dotnet nuget add source https://pkgs.dev.azure.com/IFailAt/freebsd-dotnet-runtime-nightly/_packaging/freebsd-dotnet/nuget/v3/index.json --name freebsd-dotnet

# Install SkiaSharp/HarfBuzzSharp
curl -L -o /tmp/libskiasharp.pkg https://github.com/SnapXL/freebsd-libskiasharp3/releases/download/3.119.1/libskiasharp-3.119.1_1-amd64.pkg
sudo pkg install -y /tmp/libskiasharp.pkg
rm /tmp/libskiasharp.pkg

# Install FFmpeg 8 and xvfb-run utility
curl -fsSL https://github.com/Thefrank/ffmpeg-static-freebsd/releases/download/v8.0/ffmpeg -o /usr/local/bin/ffmpeg
sudo chmod +x /usr/local/bin/ffmpeg

curl -fsSL http://svn.exactcode.de/t2/trunk/package/xorg/xorg-server/xvfb-run.sh -o /usr/local/bin/xvfb-run
sudo chmod +x /usr/local/bin/xvfb-run

# Verify installation
dotnet --info
```

# Building from Source

### System Requirements for Compiling

To successfully compile SnapX from source, ensure your system meets the following requirements:

* **Memory (RAM):** A minimum of **8 GiB** free memory is required during the compilation process.
* **Disk Space:** At least **15 GiB** of free disk space is recommended, preferably on a Solid State Drive (SSD) for optimal compilation speed.

```bash
git clone https://github.com/SnapXL/SnapX
cd SnapX
./build.sh # Linux/macOS
.\build.ps1 # Windows
Output/snapx-ui/snapx-ui # Run SnapX.Avalonia
# Nothing is stopping you from using regular .NET building tools.
# dotnet publish -c Release ./SnapX.slnx
# SnapX.Avalonia/bin/Release/net10.0/linux-x64/publish/snapx-ui
```
