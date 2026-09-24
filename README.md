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

## 性能ログの分析

起動・更新・Roblox 起動の各段階について、Logs に `[Performance]` 形式で測定値が記録されます。複数回の起動後に、次のコマンドで p50/p95 とリソース使用量を集計できます。

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\\Scripts\\analyze-performance.ps1 `
  -LogPath "$env:LOCALAPPDATA\\Zeamistrap\\Logs"
```

### Roblox プロセスの測定

Roblox を起動した状態で、CPU・メモリ・Handle・Threadを時系列で採取できます。

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\\Scripts\\measure-roblox-performance.ps1 `
  -ProcessId <RobloxPID> `
  -DurationSeconds 60
```

FPS/frametime が必要な場合は、Windows Performance Recorder の GPU プロファイルを使用します。

```powershell
# Roblox のゲーム画面を表示した状態で実行
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\\Scripts\\capture-wpr-gpu.ps1 `
  -ProcessId <RobloxPID> `
  -DurationSeconds 60 `
  -OutputPath .\\Roblox-gpu.etl
```

生成された `Roblox-gpu.etl` は Windows Performance Analyzer で開き、Present/FPS/Frame time を分析できます。

### 実行中のRobloxを保護する更新処理

自動package upgradeは、実行中の `RobloxPlayerBeta` を強制終了しません。Robloxが起動中の場合は更新を保留し、ユーザーがRobloxを終了してから次回起動時に再試行します。コンテキストメニューの「Robloxを閉じる」とアンインストールの確認ダイアログは、明示的なユーザー操作として従来どおり終了処理を行います。

### PresentMon による FPS/Frame time の自動集計

WPA を手動操作せず presenting frame の CPU/GPU 時間を集計する場合は、Intel PresentMon を使用できます。ETW GPU セッションを起動するため、UAC で管理者権限を承認してください。

```powershell
# Roblox のゲーム画面を表示した状態で実行
presentmon.exe --process_id <RobloxPID> --timed 60 --terminate_after_timed `
  --output_file .\roblox-presentmon.csv --v2_metrics --no_console_stats

powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\\Scripts\\analyze-presentmon.ps1 `
  -CsvPath .\\roblox-presentmon.csv `
  -ProcessId <RobloxPID> `
  -WarmupSeconds 2
```

`analyze-presentmon.ps1` は swap chain ごとの平均 FPS、CPU frame interval、p50/p95/p99、1% Low、CPU busy、GPU time、display latency を表示します。`FrameTime` は Roblox/CPU 側の busy time として、連続する `CPUStartTime` の差分を frame interval として另行集計します。

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
