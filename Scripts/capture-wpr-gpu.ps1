param(
    [int]$DurationSeconds = 60,
    [string]$OutputPath = "$env:USERPROFILE\Roblox-gpu.etl",
    [string]$ProcessName = 'RobloxPlayerBeta',
    [int]$ProcessId = 0
)

$ErrorActionPreference = 'Stop'

if ($DurationSeconds -lt 1) {
    throw 'DurationSeconds must be at least 1.'
}

if ($ProcessId -gt 0) {
    $targetProcess = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
}
else {
    $targetProcess = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue |
        Sort-Object StartTime |
        Select-Object -First 1
}

if ($null -eq $targetProcess) {
    $target = if ($ProcessId -gt 0) { "ID $ProcessId" } else { "'$ProcessName'" }
    throw "Roblox process $target was not found. Start Roblox before running this script."
}

$windowHandle = 0
$windowDeadline = (Get-Date).AddSeconds(30)
while ((Get-Date) -lt $windowDeadline) {
    try {
        $targetProcess.Refresh()
        if ($targetProcess.HasExited) {
            break
        }

        $windowHandle = $targetProcess.MainWindowHandle
    }
    catch [System.InvalidOperationException] {
        break
    }

    if ($windowHandle -ne 0) {
        break
    }

    Start-Sleep -Milliseconds 500
}

if ($windowHandle -eq 0) {
    throw "Roblox PID $($targetProcess.Id) has no visible window. Start the game window before capturing GPU data."
}

Write-Host "Target process: PID $($targetProcess.Id), window handle $windowHandle"

$outputDirectory = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}

Write-Host 'Starting Windows Performance Recorder GPU profile...'
wpr.exe -start GPU -filemode

if ($LASTEXITCODE -ne 0) {
    throw "WPR failed to start (exit code $LASTEXITCODE)."
}

try {
    Write-Host "Recording for $DurationSeconds seconds..."
    Start-Sleep -Seconds $DurationSeconds
    wpr.exe -stop $OutputPath

    if ($LASTEXITCODE -ne 0) {
        throw "WPR failed to stop (exit code $LASTEXITCODE)."
    }

    Write-Host "Trace saved to $OutputPath"
}
catch {
    wpr.exe -cancel | Out-Null
    throw
}
