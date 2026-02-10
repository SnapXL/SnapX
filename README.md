<p align="center">
  <a href="https://github.com/SnapXL/SnapX">
    <img src="./.github/Linux.png" alt="SnapX Banner" />
  </a>
</p>
<h1 align="center">SnapX</h1>
<h3 align="center">Capture, share, and boost productivity. All in one.</h3>
<h3 align="center">Built on the foundations of <a href="https://getsharex.com">ShareX</a>, made cross-platform. </h3>
<p align="center">
  <b>Everything you love, engineered for speed. User-centric. Native. Powerful. </b>
</p>

<p align="center"><a href="https://github.com/SnapXL/SnapX"><img src="./.github/Screenshot.webp" alt="SnapX Interface" /></a></p>

<br>
<div align="center">
  <a href="https://github.com/SnapXL/SnapX/actions/workflows/build.yml"><img src="https://badgen.net/github/checks/SnapXL/SnapX/develop?label=Build" alt="GitHub Workflow Status" /></a>
  <a href="./LICENSE.md"><img src="https://badgen.net/github/license/SnapXL/SnapX?color=green" alt="License" /></a>
  <a href="https://github.com/SnapXL/SnapX/releases/latest"><img src="https://badgen.net/github/release/SnapXL/SnapX?label=Release&color=green" alt="Release" /></a>
  <a href="https://github.com/SnapXL/SnapX/releases/latest"><img src="https://badgen.net/github/assets-dl/SnapXL/SnapX?label=Downloads&color=green" alt="Downloads" /></a>
  <br>
  <br>
  <a href="https://aur.archlinux.org/pkgbase/snapx"><img src="https://raw.githubusercontent.com/ok-coder1/devins-badges-contrib/refs/heads/v3/assets/compact/available/aur_vector.svg" height="40" alt="AUR Package Base" /></a>
  <a href="https://github.com/BrycensRanch/homebrew-repo"><img src="https://raw.githubusercontent.com/ok-coder1/devins-badges-contrib/refs/heads/v3/assets/compact/available/homebrew_vector.svg" height="40" alt="My Homebrew Tap" /></a>
  <a href="https://snapcraft.io/ui-snapx"><img src="https://img.shields.io/badge/Available on Snapcraft-E95420?style=flat&logo=snapcraft&logoColor=white&labelColor=5E2750" height="40" width="244" alt="Get it from the Snap Store" /></a>
  <a href="https://github.com/SnapXL/SnapX/wiki/Adding-DEB-RPM-repository"><img src="https://img.shields.io/badge/RPMs%2FDEBs_repository-51A2DA?style=flat&logo=linux&logoColor=white&labelColor=294172" height="40" Width="244" alt="Get it from our repository" /></a>
  <a href="https://nightly.link/SnapXL/SnapX/workflows/build/develop?preview"><img src="https://img.shields.io/badge/Flatpak available-51A2DA?style=flat&logo=flatpak&logoColor=white&labelColor=294172" height="40" Width="244" alt="Get it from our repository" /></a>
  <br>
  <a href="https://discord.gg/ys3ZCzttVQ"><img src="https://cdn.jsdelivr.net/npm/@intergrav/devins-badges@3/assets/compact/social/discord-singular_vector.svg" height="40" alt="Discord" /></a>
  <a href="https://ko-fi.com/BrycensRanch"><img src="https://cdn.jsdelivr.net/npm/@intergrav/devins-badges@3/assets/compact/donate/kofi-singular_vector.svg" height="40" alt="Support me on Ko-fi" /></a>
  <a href="https://paypal.me/BrycensRanch"><img src="https://cdn.jsdelivr.net/npm/@intergrav/devins-badges@3/assets/compact/donate/paypal-singular_vector.svg" height="40" alt="Support me on PayPal" /></a>
</div>
<br>

> [!CAUTION]
> SnapX is in **_Early Access_**.
> The core capture and upload engine is stable and ready for daily use. However, the Image Editor is still in the works.

## Feature-wise

[//]: # (- Elegance in user interfaces by separating essential settings from advanced or intermediate functionality)
- Supporting high DPI screens
- Screenshots on an HDR monitor aren't blown out<sup>[1]</sup>
- Cross-platform OCR powered by [**PaddleOCR**](https://github.com/PaddlePaddle/PaddleOCR) for industry-leading precision. Experience accuracy that [**outperforms**](https://intuitionlabs.ai/articles/non-llm-ocr-technologies#paddleocr--industrial-grade-deep-ocr-baidu) PowerToys OCR, ShareX, Tesseract, and Windows' built in OCR.

> [1] When tested on KDE Plasma Wayland 6.2.90 with HDR, the resulting screenshots' colors were not blown out. Your mileage may vary.

## Supported Desktop Environments

This application relies on XDG portals to handle screenshots in a secure and desktop-agnostic way. It is actively tested on:

- **KDE Plasma** <img src="https://kde.org/images/plasma.svg" alt="KDE Plasma Logo" height="25" width="25"/>
- **GNOME** <img src="https://github.com/user-attachments/assets/97fe5498-ea11-42af-ab09-f4c5f46ef4b0" alt="GNOME Logo" height="25" width="25"/>

We also use direct X11 screenshot capture on X based environments.

> [!TIP]
> Other desktop environments or Wayland compositors, like Budgie, Cinnamon, MATE, Hyprland, and any others that have the right screenshot portal, should work, but they haven't been officially tested.

## Packaging

See our quick start testing guide here to learn [how to test](https://github.com/SnapXL/SnapX/wiki/Testing) SnapX.

SnapX is packaged on:

<!-- - [Flathub](https://flathub.org/en/apps/io.github.SnapXL.SnapX) <img src="https://github.com/user-attachments/assets/cb95b73e-8201-4750-b8b9-25b066574e12" alt="Flathub Logo" height="25" width="25" /> [PENDING] -->

- **AUR:** [`snapx-ui`](https://aur.archlinux.org/packages/snapx-ui) <img src="https://github.com/user-attachments/assets/e9e43ff4-118a-4db1-8f71-9489adafcbf9" alt="Arch Linux Logo" height="30" width="30" />
- **Snapcraft:** [`ui-snapx`](https://snapcraft.io/ui-snapx) <img src="https://upload.wikimedia.org/wikipedia/en/a/ae/Snapcraft-logo-bird.svg" alt="Snapcraft Logo" height="25" width="25"/>
- **Homebrew:**  [BrycensRanch/homebrew-repo](https://github.com/BrycensRanch/homebrew-repo) <img src="https://raw.githubusercontent.com/Homebrew/brew.sh/main/assets/img/homebrew.svg" alt="Homebrew Logo" height="25" width="25" />
- **DEB/RPM Repo:** [Setup Instructions](https://github.com/SnapXL/SnapX/wiki/Adding-DEB-RPM-repository)

**Flatpak** (Flathub pending)
- **x86_64:** [Download](https://nightly.link/SnapXL/SnapX/workflows/build/develop/io.github.SnapXL.SnapX-x86_64.flatpak)
- **aarch64:** [Download](https://nightly.link/SnapXL/SnapX/workflows/build/develop/io.github.SnapXL.SnapX-aarch64.flatpak)

Additionally, you can download nightly builds from [here](https://nightly.link/SnapXL/SnapX/workflows/build/develop?preview).


## Technical Details

- It uses [.NET 10](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview) and [ImageSharp](https://docs.sixlabors.com/articles/imagesharp/?tabs=tabid-1) (cross-platform image library).
- It uses [SQLite](https://www.sqlite.org/about.html) for [image metadata like image hashes & history](https://github.com/SnapXL/SnapX/issues/28).
- UI is GPU-accelerated, leading to a more responsive UI & yet less CPU usage while navigating the UI. (Fixes low performance on 4K screens with a weak CPU)
- Respects [XDG directory specification](https://specifications.freedesktop.org/basedir-spec/latest/), Symlinks ~/Documents/SnapX to respective config/data directory on Linux/macOS.
- Uses [Direct3D11](https://learn.microsoft.com/en-us/windows/win32/direct2d/comparing-direct2d-and-gdi) & [WinRT](https://learn.microsoft.com/en-us/windows/apps/develop/platform/csharp-winrt/) to capture on Windows, [XCap](https://github.com/nashaofu/xcap) on macOS, and [XDG Portals](https://flatpak.github.io/xdg-desktop-portal/) on Linux.
- Supports PNG (including animated variant), WEBP (including animated variant), AVIF, JPEG, GIFs (should be smaller than your typical ShareX GIF), TIFF, and BMP image formats.
- Supports 95% of ShareX uploaders (we're a fork!).
- Allows you to fully configure SnapX via the Command Line via command flags & environment variables. Additionally, you can configure SnapX using the Windows Registry.
- Keeps compatibility with the custom uploader configuration format (.sxcu).
- As a user, you do **NOT** need to have .NET installed. Whether you're on Linux, Windows, macOS, or FreeBSD.

[//]: # (- Supports Google Photos Image Uploader after the [new API change]&#40;https://developers.googleblog.com/en/google-photos-picker-api-launch-and-library-api-updates/&#41;.)

What does this all mean? It means you'll be able to have a more **performant**, **reliable**, and **stylish** application.

You will *not* receive any support from the ShareX project for this software. \
If you have any issues with this project or would like us to add any new feature, please **open an issue** in this repository or use the [`#development`](https://discord.com/channels/1267996919922430063/1404876855861051562) channel in our [Discord](https://discord.gg/ys3ZCzttVQ).

## Building & Contributing

Contributions are welcome.
See [`BUILDING.md`](./.github/BUILDING.md) for build instructions.

The documentation for contributing can be found at [`CONTRIBUTING.md`](./.github/CONTRIBUTING.md).

## 🤝 Real People. Real Code. Soul. 💖

Free-range, organic, non-GMO, and locally sourced developers. This code was created without causing any damage to any GPUs.

<div align="center">
  <h3>👨‍💻 Espresso Consortium</h3>
    <p><em>Turning caffeine into code, and bugs into features.</em></p>
    <p>The architects, packagers, documentation writers, debuggers, and morning birds currently building SnapX.</p>
  <table border="0">
    <tr>
      <td align="center" width="150">
        <a href="https://github.com/BrycensRanch">
          <b>BrycensRanch (Lead)</b>
          <br>
          <img src="https://github.com/BrycensRanch.png?size=100" width="85" style="border-radius:50%">
        </a>
      </td>
      <td align="center" width="150">
        <a href="https://github.com/ok-coder1">
          <b>ok-coder1 (Team)</b>
          <br>
          <img src="https://github.com/ok-coder1.png?size=100" width="85" style="border-radius:50%">
        </a>
      </td>
      <td align="center" width="150">
        <a href="https://github.com/Rune580">
          <b>Rune580</b>
          <br>
          <img src="https://github.com/Rune580.png?size=100" width="85" style="border-radius:50%">
        </a>
      </td>
      <td align="center" width="150">
        <a href="https://github.com/norz3n">
          <b>norz3n</b>
          <br>
          <img src="https://github.com/norz3n.png?size=100" width="85" style="border-radius:50%">
        </a>
      </td>
    </tr>
  </table>
</div>

<div align="center">
  <h3>💸 The Caffeine & Infrastructure Cartel</h3>
  <p>These charitable people support our early morning server maintenance and coding sessions. Their donations literally keeps the lights on and the compilers warm.</p>

  <table border="0">
    <tr>
      <td align="center" width="150">
        <a href="https://github.com/Rsslone">
          <b>Rsslone (Tommy)</b>
          <br>
          <img src="https://github.com/Rsslone.png?size=100" width="85" style="border-radius:50%">
        </a>
      </td>
      <td align="center" width="150">
        <a href="https://github.com/Skorlok">
          <b>Skorlok</b>
          <br>
          <img src="https://github.com/Skorlok.png?size=100" width="85" style="border-radius:50%">
        </a>
      </td>
      <td align="center" width="150">
        <a href="https://github.com/Abdullah16M">
          <b>Abdullah16M</b>
          <br>
          <img src="https://github.com/Abdullah16M.png?size=100" width="85" style="border-radius:50%">
        </a>
      </td>
    </tr>
  </table>
<a href="https://github.com/sponsors/BrycensRanch">
  <img alt="GitHub Sponsors" src="https://img.shields.io/github/sponsors/BrycensRanch?logo=github&logoColor=pink&label=GitHub Sponsors">
</a>
<a href="https://liberapay.com/BrycensRanch">
  <img src="https://badgen.net/liberapay/receives/BrycensRanch?label=Liberapay" alt="Liberapay Receive Badge" />
</a>
<a href="https://ko-fi.com/BrycensRanch">
  <img src="https://badgen.net/badge/KoFi/Buy%20me%20a%20coffee/ff5f5f?icon=kofi" alt="Ko-fi" />
</a>
</div>

<br>

<div align="center">
  <h3>🔬 Battle-tested by a select few</h3>
  <p>Our code isn't compiled. It's <strong>dry-aged</strong>.</p>
  <p>"Hallucinated" bug fixes are not what we do. We use a more conventional approach: look at a stack trace until someone breaks down in tears.</p>
  <p>SnapX was built by the brave ones who wiped their eyes…<br>and kept clicking.</p>
<table border="0">
  <tr>
    <td align="center" width="120">
      <a href="https://discord.com/users/164445443680370688">
        <b>Horo</b>
        <br>
        <img src="https://cdn.discordapp.com/avatars/164445443680370688/c3903fec7b8194538f361f07f8158dc3.webp?size=80" width="70" style="border-radius:50%">
      </a>
    </td>
    <td align="center" width="120">
      <a href="https://discord.com/users/182579271095681024">
        <b>Freako95</b>
        <br>
        <img src="https://cdn.discordapp.com/avatars/182579271095681024/6d06b134101abdee90f324ce9bd5728a.webp?size=80" width="70" style="border-radius:50%">
      </a>
    </td>
    <td align="center" width="120">
      <a href="https://discord.com/users/182674215424622592">
        <b>トミー (tommy.sama)</b>
        <br>
        <img src="https://cdn.discordapp.com/avatars/182674215424622592/702f394eb560bfb04fa64aaf371720cd.webp?size=80" width="70" style="border-radius:50%">
      </a>
    </td>
    <td align="center" width="120">
      <a href="https://discord.com/users/224868029610197002">
        <b>Tobi</b>
        <br>
        <img src="https://cdn.discordapp.com/avatars/224868029610197002/b25365f9df561c83ea39c948f561ffd4.webp?size=80" width="70" style="border-radius:50%">
      </a>
    </td>
    <td align="center" width="120">
      <a href="https://discord.com/users/277518977033437185">
        <b>Skorlok</b>
        <br>
        <img src="https://cdn.discordapp.com/avatars/277518977033437185/967acee9feb87a326dc8a98e32514912.webp?size=80" width="70" style="border-radius:50%">
      </a>
    </td>
  </tr>
  <tr>
    <td align="center" width="120">
      <a href="https://discord.com/users/354370875878801419">
        <b>Tape1</b>
        <br>
        <img src="https://cdn.discordapp.com/avatars/354370875878801419/6b2cdd67625303b8991b08e2ef76d906.webp?size=80" width="70" style="border-radius:50%">
      </a>
    </td>
    <td align="center" width="120">
      <a href="https://discord.com/users/362670720972619777">
        <b>Ione 15</b>
        <br>
        <img src="https://cdn.discordapp.com/avatars/362670720972619777/9f722e792194a1728d356c7af79de67c.webp?size=80" width="70" style="border-radius:50%">
      </a>
    </td>
    <td align="center" width="120">
      <a href="https://discord.com/users/474221560031608833">
        <b>Lee</b>
        <br>
        <img src="https://cdn.discordapp.com/avatars/474221560031608833/8e447f29d00eb191cb0421fe4e91273e.webp?size=80" width="70" style="border-radius:50%">
      </a>
    </td>
    <td align="center" width="120">
      <a href="https://discord.com/users/615785223296253953">
        <b>Tari</b>
        <br>
        <img src="https://cdn.discordapp.com/avatars/615785223296253953/44ac4c5c337af5cc827ae6e9ca091ea6.webp?size=80" width="70" style="border-radius:50%">
      </a>
    </td>
    <td align="center" width="120">
      <a href="https://discord.com/users/677619920657317936">
        <b>revolume</b>
        <br>
        <img src="https://cdn.discordapp.com/avatars/677619920657317936/bbd30a5d9c1876c53f391fdd7a4d138a.webp?size=80" width="70" style="border-radius:50%">
      </a>
    </td>
  </tr>
  <tr>
    <td align="center" width="120">
      <a href="https://discord.com/users/821472922140803112">
        <b>Luna</b>
        <br>
        <img src="https://cdn.discordapp.com/avatars/821472922140803112/09023e78e4afe943e440806188f77539.webp?size=80" width="70" style="border-radius:50%">
      </a>
    </td>
  </tr>
</table>
</div>


## Roadmap

See [`Progress.md`](./.github/Progress.md).
