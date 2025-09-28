# Internal document outlining the progress and goals of the project

# Checklist

- [x] Port `NativeMessagingHost` to .NET 9 (It was a few lines of code...)
- [x] Convert history to SQLite instead of JSON. I know this is a big change, but it'd remove the typically unnecessary built-in backup feature. <https://pl-rants.net/posts/when-not-json> <https://github.com/dotnet/efcore>
- [x] Log files should be a daily log file, not a whole MONTH (wtf?)
- [x] Symlink ~/Documents/SnapX to their appropriate XDG directories to keep the familiar structure users are used to without violating the [XDG spec](https://specifications.freedesktop.org/basedir-spec/latest/).
- [ ] Create custom uploader list with reviews & privacy policies enforced & popularity filter to quality control hosts. Run service on HB VPS to automatically prune custom uploaders that aren't online 90% of the time in a week.
- [x] Package PaddleOCR for Linux properly.
- [ ] Add charts for statistics like uploading, avg image size, most used image extension, and error rate for uploading
- [ ] Ensure SnapX is the default screenshot program when installed on a new Windows installation.
- [ ] Package for popular technologies (Flatpak, RPM (Fedora/OpenSUSE), Snap, Homebrew)
- [ ] Expose the entire Core in UI (Avalonia) (Missing direct upload function & drag and drop)
- [x] Add telemetry & Aptabase is a work in progress, PR pending https://github.com/aptabase/aptabase-maui/pull/12
- [ ] Create MSI installer with [WixSharp](https://github.com/oleg-shilo/wixsharp)
- [ ] Use Microsoft's [MSIX Packaging tool](https://github.com/microsoft/msix-packaging) for MSIXBundles for Windows.
- [ ] Integrate with Windows Share https://discussions.unity.com/t/calling-windows-shareui-dialog-from-unity-on-windows-10-11-on-non-uwp-build-target/1586504
- [ ] Add to Microsoft Store
- [ ] Add to Winget, Chocolately, and Scoop
- [x] [Add to Homebrew](https://github.com/BrycensRanch/homebrew-repo)
- [ ] Make a v1.0.0 release date ETA that aligns with a ShareX release date ie (a day after a new major version)
- [ ] Add to Itch.io
- [x] [Add to AUR (see PR #56 for the initial PKGBUILD)](https://aur.archlinux.org/packages/snapx-ui)
- [ ] Add to FOSS Torrents
- [ ] Add to PortableApps.com
- [ ] Add [Jump list](https://github.com/ShareX/ShareX/issues/1106#issuecomment-596048694)
- [ ] Rework settings. Making it fit in with the WinUI style.
- [ ] Add database viewer as an built in SQLite database viewer that is able to complete operations like mass path replacement ie `C:\Users/Brycen` -> `/home/brycen`
- [ ] Make CLI UNIX friendly while keeping ShareX's CLI valid. The CLI should be able to print its usage and autocomplete.
- [ ] Add first tool, the FFMPEG 100MB video transcoder
- [ ] Port `go-keyring` to C# (Needed for not saving auth creds in plaintext, big no no )
- [ ] Search for missing files button in main window and locate missing file to allow users to fix broken entries.
- [x] Bring in XCap library in .NET and other cross-platform screen capture libraries. (This will make the port take much longer)
- [x] Remove SnapX as a fork of ShareX that can be merged into upstream. *Completed at 233 commits ahead of upstream*
- [ ] Add Tools from ShareX to SnapX. Notably, upgrade [ExifTool](https://exiftool.org/) from a "Tool" to an optional feature that shows more information, like a properties button on a screenshot's flyout menu. ImageSharp has its features.
- [ ] Add a New Tool that will transcode/reencode videos/images to a certain size based off of the options Discord provides. I believe this can be done with FFMPEG easily.
- [ ] Add first-class support for [ImgBB](https://imgbb.com/), [Mastodon](https://mastodon.social/explore), [Bluesky](https://bsky.app/), [Pcloud](https://www.pcloud.com/), [SourceBin](https://sourceb.in/), [PrivateBin](https://github.com/PrivateBin/PrivateBin/wiki/API), [LimeWire](https://limewire.com/),   and [Pixeldrain](https://pixeldrain.com/)
- [ ] Add Custom Uploader List to SnapX via a build-time HTTP Fetch, or if the file is there already, use that. Can be disabled by packagers as they need offline builds. Or they could fetch the list, or rather, JSON, as part of their build script that isn't done during packaging time. That list is then embedded into the binary, and then at runtime it is checked *again* for any new entries to said list. Thus, the functionality keeps working even in an environment where SnapX cannot access the internet.
- [ ] ~~Add automatic region detection that suggests to users in Egypt, Russia, Ukraine, and possibly more to switch their default image uploader from Imgur to ImgBB. According to my tests, ImgBB doesn't have such region blocks for Ukraine & Egypt. Russia is untested because PIA doesn't have any servers there.~~ Imgur continually gets less reliable. I am investigating making ImgBB the default uploader for now until a more suitable alternative is found.

## Studying ShareX's behavior on Windows 11 24H2

It's important to know how the program *should* behave per user expectations. As such, I've done a little recording of it.

## Rewrite

ShareX's internal code needs major refactoring and decoupling to be ready to work on Linux natively. For example, most cross platform screen capture libraries only work on X11 or hardly work at all. Hopefully, screenshotting on [Wayland](https://wayland.freedesktop.org/) can be done with DBus on .NET. <https://github.com/tmds/Tmds.DBus>

I also want to decouple *away* from a specific UI framework. \
While GTK4 does "work" on these platforms, it's significantly handicapped or unstable (on macOS). \
Keeping flexibility will be advantageous in the future, I imagine.

For screen capture, we currently use [xcap](https://github.com/nashaofu/xcap).

### SemVer & New Commit Message Standard

The version for this port has been set to 0.1.0 until the project is in a usable state. The version will be updated to 1.0.0 when the project feature is complete and ready for general use. The project begins with ShareX's version 16 code base.

For the commit messages, I will be following the [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) standard. This will allow for automatic versioning and changelog generation. This will also allow for easier tracking of changes and features.

### Implementation of auto update & controversial features

#### Auto Update

ShareX on Windows has auto update functionality. This is a feature that I would like to implement in this port. This will allow users to receive updates automatically without having to manually download and install them. This will also allow for easier distribution of updates and bug fixes. I know this doesn't bode well with Linux users. So on actual Linux packages, they will be disabled because I don't want a situation like Discord on Linux. :laughing:

![Screenshot showing Discord complaining about being outdated yet suggesting you should download their DEB package on Arch Linux](discordarchexample.png)

The idea is for SnapX to check for updates on startup. Since the goal is to have the application with one singular binary with no DLLs/.so files to worry about. Like Electron apps. It'll replace the application binary to the latest version that is the same major version. This will allow for easy updates and bug fixes to be distributed to users. Downgrades will not be allowed.

`ShareNoSnap` variant will not auto update; in fact, it shouldn't ship the code to do it at all.

#### Telemetry

I'm aiming to add telemetry to the application. \
This will allow for the collection of anonymous usage data.

This data will be used to improve the application and fix bugs. \
Coming in the form of [Sentry](https://sentry.io/) and [Aptabase](https://github.com/aptabase/aptabase).

Allowing for the automatic collection of crash reports and other useful data for debugging. Aptabase is for application analytics.

It is opt-out and can be disabled in the settings. Additionally, the `ShareNoSnap` variant will not include telemetry, as you'd expect. Nor will the code even exist for it to do so.

Telemetry is best when it represents the majority of the user base. I kindly ask you to not disable it. It's for the greater good. I know companies continue to abuse "telemetry" for their own gain, but this is not the case here. This is for the betterment of the application and the user experience. I'm not selling your data to advertisers. I'm not selling your data to anyone.

If you're concerned, view our [Privacy Policy](../packaging/PRIVACY.md)

#### Why are you doing this?

WINE is not a solution. Wine is a compatibility layer. It is not a replacement for native applications. I enjoyed using ShareX. Previous attempts to have always been to try and negate the fact that ultimately a Windows application. I hope to reuse ShareX's code with the introduction of .NET 9 and Avalonia, but with this port, it should become a cross platform application

I am also just not interested in Mono.

#### How are screenshots going to work?

I am going to use a library. I have decided to do it. I might keep the Windows code and investigate adding HDR support to it.

<https://sixlabors.com/products/imagesharp/> This library is a cross-platform library that can be used to manipulate images. This library will be used to handle images in this project.

#### Snap & Flatpak

.NET 9 SDK Snap ✅

<https://snapcraft.io/dotnet-sdk-90>

.NET 9 Flatpak SDK Extension ✅

<https://github.com/flathub/org.freedesktop.Sdk.Extension.dotnet9>

A snap package should be created easily with examples like <https://github.com/BrycensRanch/Rokon/blob/master/snapcraft.yaml>

##### Finally

````
Jaex — 03/04/2017 8:37 PM
i know countless people who want to make linux version too
but nobody willing to do it
or give up middle of it after see difficulty
so it is only on talk
````

"Talk is cheap, show me the code"—Linus Torvalds

Hence, the broadening of the scope from a Linux port to a cross-platform modern hard fork.
