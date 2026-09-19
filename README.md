<div align="center">

![](Images/header.png)
![][badge-license]
![][badge-actions]
![][badge-downloads]
[![][badge-latest]][repo-latest]
![][badge-stars]

</div>

Zeamistrapはパフォーマンス向上を目的としたRobloxのカスタムクライアントです。

不具合を見つけた場合は [issue][repo-new-issue] または[Discord](https://discord.com/invite/NnFbjSb45p)サーバーまでご報告ください。

> [!NOTE]
> Zeamistrapは**Windows 10以降**に対応したアプリケーションです。
> Mac OSや各種Linuxディストリビューションなど、その他のOSでは、
> それぞれ[AppleBlox][appleblox]と[Sober][sober]を試すことができます。

## ソースコードからのビルド

前提条件:

- Windows 10 / 11 に .NET 8 SDK がインストールされていること
  - リポジトリの `global.json` は `rollForward: latestMajor` のため、最近の
    SDK が自動的に解決されます
- `wpfui` のソースコードはリポジトリに直接含まれています
  (サブモジュールではありません。別途取得する必要はありません)

クローン:

```sh
git clone https://github.com/Zeamistrap/Zeamistrap.git
```

ビルドと実行ファイルの生成(シングルファイル・フレームワーク依存の実行ファイル):

```sh
dotnet build Zeamistrap.sln -c Release
dotnet publish .\Bloxstrap\Bloxstrap.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:PublishReadyToRun=false -o .\Publish
```

完成した実行ファイルは `Publish\Zeamistrap.exe` に出力されます。

<div align="center">

</div>

[badge-license]:   https://img.shields.io/github/license/Zeamistrap/Zeamistrap?style=flat-square
[badge-actions]:   https://img.shields.io/github/actions/workflow/status/Zeamistrap/Zeamistrap/ci-release.yml?branch=main&style=flat-square&label=builds
[badge-downloads]: https://img.shields.io/github/downloads/Zeamistrap/Zeamistrap/latest/total?style=flat-square&color=981bfe
[badge-latest]:    https://img.shields.io/github/v/release/Zeamistrap/Zeamistrap?style=flat-square&color=7a39fb
[badge-stars]:     https://img.shields.io/github/stars/Zeamistrap/Zeamistrap?style=flat-square&color=dd9900
[repo-latest]:    https://github.com/Zeamistrap/Zeamistrap/releases/latest
[repo-new-issue]: https://github.com/Zeamistrap/Zeamistrap/issues/new/choose
[bloxstrap]: https://bloxstraplabs.com
[appleblox]: https://github.com/AppleBlox/appleblox
[sober]:     https://sober.vinegarhq.org
[rovalra]:   https://www.rovalra.com
[devforum-fflags]: https://devforum.roblox.com/t/allowlist-for-local-client-configuration-via-fast-flags/3966569
