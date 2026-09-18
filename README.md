> [!CAUTION]
> The only official places to download Zeamistrap are this GitHub repository and
> our website <https://zeamistrap.app>. Any other websites offering downloads
> or claiming to be us are not controlled by us, do not download from them.

<div align="center">

![][banner-light]
![][banner-dark]

![][badge-license]
![][badge-actions]
![][badge-downloads]
[![][badge-latest]][repo-latest]
![][badge-stars]

</div>

Zeamistrap is a custom bootstrapper for Roblox based on [Bloxstrap][bloxstrap].
It provides additional features to enhance your experience.

If you found any bugs, please [open an issue here][repo-new-issue].

> [!NOTE]
> Zeamistrap is an application for **Windows 10 and above.** For other operating
> systems, such as Mac OS and various Linux distributions, you can try
> [AppleBlox][appleblox] and [Sober][sober] respectively.

## Features

- Detailed server information using [RoValra][rovalra]'s API
- Support for Roblox Studio
- Unhidden FastFlags editor
  - You cannot apply FastFlags not present in the allowlist. This does not
    affect Roblox Studio. [Learn more][devforum-fflags]
- Global Basic Settings editor
  - Ability to increase frame rate cap, toggle quality levels and more
- Zeamistrap's own game invites
  - Try it out now — this link will lead you to Crossroads (don't turn left!):
    <https://zeamistrap.app/v1/joingame?placeId=1818>
- Cache cleaner, channel switcher and many more

## Building from source

Prerequisites:

- Windows 10/11 with the .NET 8 SDK installed (the repository's `global.json`
  resolves any recent SDK via `rollForward: latestMajor`)
- The `wpfui` submodule must be checked out before the first build:

```sh
git clone --recurse-submodules https://github.com/Zeamistrap/Zeamistrap.git
# or, when the repository was already cloned without submodules:
git submodule update --init
```

Build and publish a single-file, framework-dependent executable:

```sh
dotnet build Zeamistrap.sln -c Release
dotnet publish .\Bloxstrap\Bloxstrap.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:PublishReadyToRun=false -o .\Publish
```

The finished binary will be at `Publish\Zeamistrap.exe`.

## Special thanks

- [Valra](https://github.com/NotValra) for providing their API
- Other independent contributors

<div align="center">

![][repo-showcase-light]
![][repo-showcase-dark]

</div>

[banner-light]: https://github.com/Zeamistrap/Zeamistrap/raw/main/Images/Zeamistrap-Light.png#gh-light-mode-only
[banner-dark]:  https://github.com/Zeamistrap/Zeamistrap/raw/main/Images/Zeamistrap-Dark.png#gh-dark-mode-only

[badge-license]:   https://img.shields.io/github/license/Zeamistrap/Zeamistrap?style=flat-square
[badge-actions]:   https://img.shields.io/github/actions/workflow/status/Zeamistrap/Zeamistrap/ci-release.yml?branch=main&style=flat-square&label=builds
[badge-downloads]: https://img.shields.io/github/downloads/Zeamistrap/Zeamistrap/latest/total?style=flat-square&color=981bfe
[badge-latest]:    https://img.shields.io/github/v/release/Zeamistrap/Zeamistrap?style=flat-square&color=7a39fb
[badge-stars]:     https://img.shields.io/github/stars/Zeamistrap/Zeamistrap?style=flat-square&color=dd9900

[repo-latest]:    https://github.com/Zeamistrap/Zeamistrap/releases/latest
[repo-new-issue]: https://github.com/Zeamistrap/Zeamistrap/issues/new/choose

[repo-showcase-dark]:  https://github.com/Zeamistrap/Zeamistrap/raw/main/Images/Showcase-Dark.png#gh-dark-mode-only
[repo-showcase-light]: https://github.com/Zeamistrap/Zeamistrap/raw/main/Images/Showcase-Light.png#gh-light-mode-only

[bloxstrap]: https://bloxstraplabs.com
[appleblox]: https://github.com/AppleBlox/appleblox
[sober]:     https://sober.vinegarhq.org
[rovalra]:   https://www.rovalra.com

[devforum-fflags]: https://devforum.roblox.com/t/allowlist-for-local-client-configuration-via-fast-flags/3966569