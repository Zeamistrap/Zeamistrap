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

### 実行中のRobloxを保護する更新処理

自動package upgradeは、実行中の `RobloxPlayerBeta` を強制終了しません。Robloxが起動中の場合は更新を保留し、ユーザーがRobloxを終了してから次回起動時に再試行します。コンテキストメニューの「Robloxを閉じる」とアンインストールの確認ダイアログは、明示的なユーザー操作として従来どおり終了処理を行います。

## 開発中の更新

### アプリ内更新画面

- 起動時の初期化、package取得、Watcher、HTTP、JSON永続化を軽量化
- 実行中のRobloxを強制終了しない更新処理
- GitHub Releases APIから最新リリースを取得し、リリースノートをアプリ内画面に表示
- GitHubのMarkdown表記（見出し、リスト、リンクなど）を反映
- リリースノートをコードブロック風の中枠で表示
- Zeamistrapロゴをウィンドウアイコンと画面左側に表示
- 現在のバージョンから最新バージョンへの矢印表示
- 更新確認・更新完了をアプリ内画面で統一
- 更新後にGitHubのウェブサイトへ自動遷移しない安全化

### 診断機能の整理

性能診断用の補助機能として次を削除しました。

- `Bloxstrap/PerformanceMetrics.cs`
- `Scripts/EtlTraceAnalysis/`
- `Scripts/analyze-performance.ps1`
- `Scripts/analyze-presentmon.ps1`
- `Scripts/capture-wpr-gpu.ps1`
- `Scripts/measure-roblox-performance.ps1`
- 関連する性能計測ログとREADMEの診断手順

`Performance`タブと電源プラン設定は機能を維持しています。通常的な例外・更新・状態通知用の `Logger` も引き続き使用します。

> 現在の開発ツリーの内容であり、次回リリース候補の変更です。


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
