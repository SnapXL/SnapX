<p align="center"><a href="https://github.com/BrycensRanch/SnapX/blob/351bd299dfec4fe20b630900319b61060b606eb3/.github/Logo"><img src="https://github.com/BrycensRanch/SnapX/blob/6b74876359b8bc217cf165e3f0a22cdf39f8c407/.github/Linux.png" alt="SnapX Banner"/></a></p>
<h1 align="center">SnapX</h1>
<h3 align="center">Capture, share, and boost productivity. All in one.</h3>
<br>
<div align="center">
  <a href="https://github.com/BrycensRanch/SnapX/actions/workflows/build.yml"><img src="https://img.shields.io/github/actions/workflow/status/BrycensRanch/SnapX/build.yml?branch=develop&label=Build&cacheSeconds=3600" alt="GitHub Workflow Status"/></a>
  <a href="./LICENSE.md"><img src="https://img.shields.io/github/license/BrycensRanch/SnapX?label=License&color=brightgreen&cacheSeconds=3600" alt="License"/></a>
  <a href="https://github.com/BrycensRanch/SnapX/releases/latest"><img src="https://img.shields.io/github/v/release/BrycensRanch/SnapX?label=Release&color=brightgreen&cacheSeconds=3600" alt="Release"/></a>
  <a href="https://github.com/BrycensRanch/SnapX/releases/latest"><img src="https://img.shields.io/github/downloads/BrycensRanch/SnapX/total?label=Downloads&cacheSeconds=3600" alt="Downloads"/></a>
  <a href="https://discord.gg/ys3ZCzttVQ"><img src="https://img.shields.io/discord/1267996919922430063?label=Discord&cacheSeconds=3600" alt="Discord Server"/></a>
</div>
<br>
<p align="center"><a href="https://github.com/BrycensRanch/SnapX"><img src="https://getsharex.com/img/ShareX_Screenshot.png" alt="Repo"/></a></p>

> [!CAUTION]
> **This project is _under development_ and is _not_ ready for use.**

> [!NOTE]
> **DISCLAIMER:** SnapX is a [hard fork](https://producingoss.com/en/forks.html) of the Windows application [ShareX](https://github.com/ShareX/ShareX).

## Feature-wise

- SnapX is a cross-platform application.
- Elegance in user interfaces by separating essential settings from advanced or intermediate functionality
- Supporting high DPI screens
- Screenshots on an HDR monitor aren't blown out<sup>[1]</sup>
- Cross-platform OCR powered by [PaddleOCR](https://github.com/PaddlePaddle/PaddleOCR/blob/main/README_en.md) that [rivals PowerToys OCR, ShareX OCR, & Windows 10 built in OCR in accuracy](https://toon-beerten.medium.com/ocr-comparison-tesseract-versus-easyocr-vs-paddleocr-vs-mmocr-a362d9c79e66)

> [1] When tested on KDE Plasma Wayland 6.2.90 with HDR, the resulting screenshots' colors were not blown out. Your mileage may vary.

## Technical Details

- It uses [.NET 9](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9/overview), [ImageSharp](https://docs.sixlabors.com/articles/imagesharp/?tabs=tabid-1) (cross-platform image library)
- Dependency on Newtonsoft.JSON dropped, traded out for [more strict yet performant System.Text.Json](https://dev.to/samira_talebi_cca34ce28b8/newtonsoftjson-vs-systemtextjson-in-net-80-which-should-you-choose-26a3)
- And it *will* use [SQLite](https://www.sqlite.org/about.html) to [ image metadata like image hashes & history](https://github.com/BrycensRanch/SnapX/issues/28) by default yet [~~keeping JSON as an option~~](https://github.com/BrycensRanch/SnapX/issues/28#issuecomment-2764614506).
- The UI is now defined in a more modern, declarative style using MVVM and XAML, providing a clear improvement over the older WinForms approach.
- UI is GPU accelerated, leading to a more responsive UI & yet less CPU usage while navigating the UI. (Fixes low performance on 4K screens with a weak CPU)
- Respects [XDG directory specification](https://specifications.freedesktop.org/basedir-spec/latest/), Symlinks ~/Documents/SnapX to respective config/data directory on Linux/macOS
- Uses [Direct3D11](https://learn.microsoft.com/en-us/windows/win32/direct2d/comparing-direct2d-and-gdi) & [WinRT](https://learn.microsoft.com/en-us/windows/apps/develop/platform/csharp-winrt/) to capture on Windows, [XCap](https://github.com/nashaofu/xcap) on macOS, and [XDG portals](https://flatpak.github.io/xdg-desktop-portal/) on Linux.
- Supports PNG (including animated variant), WEBP (including animated variant), JPEG, GIFs (should be smaller than your typical ShareX GIF), TIFF, and BMP image formats.
- Supports 95% of ShareX uploaders (we're a fork!!)
- Supports Google Photos Image Uploader after the [new API change](https://developers.googleblog.com/en/google-photos-picker-api-launch-and-library-api-updates/).
- The ability to fully configure SnapX via the Command Line via command flags & environment variables. Additionally, you can configure SnapX using the Windows Registry.
- Additionally, all uploaders are now forced to use HTTPS <2.0 & *optionally* use TLS 1.3 out of the box.
- Keeps compatibility with the custom uploader configuration format (.sxcu)
- As a user, you do **NOT** need to have .NET installed. Whether you're on Linux, Windows, or macOS.

What does this all mean? It means you'll be able to have a more **performant**, **reliable**, and *modern* application.

You will *not* receive any support from the ShareX project for this software. \
If you have any issues with this project, please **open an issue** in this repository.

However, it's important to note that this project is maintained by volunteers. \
We may not be able to provide support for all issues. \
We will do our best to help you, but we cannot guarantee that we will be able to resolve your issue.

<p align="center"> For further information, please check the source code.</p>

## Supported Linux Distributions

This project is built on Ubuntu 24.04 and is tested on the following distributions:

- **F**edora 41+
- **U**buntu 24.04+

> [!NOTE]
> If you're using a different distribution, there will be a Flatpak package available when possible. If you're using a distribution that doesn't support Flatpak, you can [build the project from source](#building-from-source).

## Other platforms

I initially started this project with one main goal: ShareX on Linux on Wayland. \
I realized my work could be used on other platforms such as macOS or Windows...

That's why SnapX.Avalonia was created.

Powered by [FluentAvalonia](https://github.com/amwx/FluentAvalonia), it *should* look something like this. \
Screenshot from [FluentSearch](https://github.com/adirh3/Fluent-Search): ![Screenshot of the FluentSearch application that looks like a modern native Windows application](.github/image.png)

## Packages for testers

SnapX is not yet in a usable state. These packages are provided for making testing easier.

SnapX is packaged on:

- [COPR](https://copr.fedorainfracloud.org/coprs/brycensranch/snapx/)

- [AUR](https://aur.archlinux.org/packages/snapx-ui)

- [My Homebrew Tap](https://github.com/BrycensRanch/homebrew-repo) (Thank you [@ok-coder1](https://github.com/ok-coder1)!)

Additionally, you can download nightly builds from [here](https://nightly.link/BrycensRanch/SnapX/workflows/build/develop?preview).

## Development Dependencies

- `git`
- `dotnet-sdk-9.0`
- `ffmpeg` (7)
- `rust` & `cargo` (<1.80) (macOS only, the rest use SharpCapture)
- `clang`
- `zlib-devel`

#### IDE of Choice

JetBrains Rider is the recommended IDE. It works on Linux, Windows, and macOS. It's free for noncommercial use.

<a href="https://www.jetbrains.com/rider/" target="_blank">
  <img
    src="https://github.com/user-attachments/assets/96b8e44e-47b3-4850-b4f3-c4e7ed8dd385"
    alt="JetBrains Rider - The world's most loved .NET and game dev IDE"
    style="width: 400px; height: auto;" />
</a>

### Fedora 41+ 🌟

```bash
sudo dnf in -y git dotnet-sdk-aot-9.0 /usr/bin/ffmpeg
```

### Ubuntu 24.04+ ⚡

```bash
sudo apt update && sudo apt install -y software-properties-common
sudo add-apt-repository ppa:dotnet/backports -y # Ubuntu 24.04 doesn't have .NET 9 packaged. Do not add this PPA on Ubuntu 24.10+
sudo add-apt-repository ppa:ubuntuhandbook1/ffmpeg7 -y # Ubuntu 24.04 doesn't have FFMPEG 7 packaged.
sudo apt install -y git dotnet-sdk-9.0 ffmpeg clang zlib1g-dev libsm6
```

### Windows 10 22H2+ 🪟

End of life Windows versions are not supported. For example, Windows 11 22H2 is at its EOL and, thus, unsupported.

SnapX now uses the Windows SDK to generate C# Windows API binding code. \
You need the Windows 11 SDK `10.0.26100.0`. \
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

### macOS Ventura+ (13) 🍎

End of life macOS versions are not supported. For example, macOS Monterey is at its EOL and thus, unsupported.

#### Using the script to install the .NET SDK from the .NET team makes sure you don't run into homebrew .NET weirdness with Rider not detecting it.

```zsh
xcode-select --install
brew install ffmpeg@7 rust
git --version # If prompted to install Git, do it.
exec $SHELL -l
```
> [!TIP]
> If you're using MacPorts, run this instead of the `brew install` command:
> ```zsh
> sudo port selfupdate
> sudo port install ffmpeg7 cargo
> ```

## Building from Source

> [!CAUTION]
> **Only do this if you're a developer**; you should have a backup of all your ShareX/SnapX data. \
> I do mean it when I say the project isn't ready for use.

> [!IMPORTANT]
> Additionally, it seems SnapX [hasn't been able to create the configuration file(s) it expects](https://github.com/BrycensRanch/SnapX/issues/66). \
> I've been testing with my ShareX configuration. You should place it in the configuration directory that it expects.
>
> On **Linux**, it's `~/.config/SnapX`
>
> On **Windows**, it's `%USERPROFILE%\Documents\SnapX`
>
> On **macOS**, it's `~/Library/Application Support/SnapX`

```bash
git clone https://github.com/BrycensRanch/SnapX
cd SnapX
./build.sh # Linux/macOS
.\build.ps1 # If on Windows
Output/snapx-ui/snapx-ui # Run SnapX.Avalonia
# Nothing is stopping you from using regular .NET building tools
# dotnet publish -c Release ./SnapX.sln
# SnapX.Avalonia/bin/Release/net9.0/linux-x64/publish/snapx-ui
```

## Contributions

Contributions are welcome. The documentation for contributing is a work in progress, but here is a [rough draft](./.github/CONTRIBUTING.md).

## Donators 💖

- [Skorlok](https://github.com/Skorlok)
- [Abdullah16M](https://github.com/Abdullah16M)

Thank you so much!

### Roadmap

See [Progress.md](./.github/Progress.md)
