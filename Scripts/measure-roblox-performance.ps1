param(
    [string]$ProcessName = 'RobloxPlayerBeta',
    [int]$ProcessId = 0,
    [int]$DurationSeconds = 60,
    [ValidateRange(100, 60000)]
    [int]$IntervalMilliseconds = 1000,
    [string]$OutputPath
)

if ($DurationSeconds -lt 1) {
    Write-Error 'DurationSeconds must be at least 1.'
    exit 1
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path (Get-Location) ("roblox-performance-{0}.csv" -f (Get-Date -Format 'yyyyMMdd-HHmmss'))
}

$outputDirectory = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}

if ($ProcessId -gt 0) {
    $process = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
}
else {
    $process = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue |
        Sort-Object StartTime |
        Select-Object -First 1
}

if ($null -eq $process) {
    $target = if ($ProcessId -gt 0) { "ID $ProcessId" } else { "'$ProcessName'" }
    Write-Error "Roblox process $target was not found. Start Roblox before running this script."
    exit 1
}

$logicalProcessorCount = [Environment]::ProcessorCount
$samples = [System.Collections.Generic.List[object]]::new()
$previousCpuSeconds = $process.TotalProcessorTime.TotalSeconds
$previousTimestamp = [DateTime]::UtcNow
$sampleCount = [math]::Max(1, [math]::Ceiling($DurationSeconds * 1000 / $IntervalMilliseconds))

Write-Host "Monitoring PID $($process.Id) ($ProcessName) for approximately $DurationSeconds seconds..."

for ($index = 0; $index -lt $sampleCount; $index++) {
    Start-Sleep -Milliseconds $IntervalMilliseconds

    try {
        $process.Refresh()

        if ($process.HasExited) {
            break
        }

        $timestamp = [DateTime]::UtcNow
        $elapsedSeconds = ($timestamp - $previousTimestamp).TotalSeconds
        $cpuSeconds = $process.TotalProcessorTime.TotalSeconds
        $cpuDelta = $cpuSeconds - $previousCpuSeconds
        $cpuPercent = if ($elapsedSeconds -gt 0) {
            ($cpuDelta / $elapsedSeconds / $logicalProcessorCount) * 100
        } else {
            0
        }

        $samples.Add([pscustomobject]@{
            TimestampUtc = $timestamp.ToString('o')
            ProcessId = $process.Id
            CpuPercent = [math]::Round($cpuPercent, 2)
            WorkingSetMb = [math]::Round($process.WorkingSet64 / 1MB, 2)
            PrivateMb = [math]::Round($process.PrivateMemorySize64 / 1MB, 2)
            Handles = $process.HandleCount
            Threads = $process.Threads.Count
        })

        $previousCpuSeconds = $cpuSeconds
        $previousTimestamp = $timestamp
    }
    catch [System.InvalidOperationException] {
        break
    }
}

if ($samples.Count -eq 0) {
    Write-Error 'No samples were collected; the process may have exited immediately.'
    exit 1
}

$samples | Export-Csv -LiteralPath $OutputPath -NoTypeInformation -Encoding UTF8

function Get-Percentile([double[]]$Values, [double]$Percentile) {
    $sorted = @($Values | Sort-Object)
    if ($sorted.Count -eq 1) {
        return $sorted[0]
    }

    $position = ($sorted.Count - 1) * $Percentile
    $lower = [math]::Floor($position)
    $upper = [math]::Ceiling($position)
    if ($lower -eq $upper) {
        return $sorted[$lower]
    }

    return $sorted[$lower] + (($sorted[$upper] - $sorted[$lower]) * ($position - $lower))
}

$cpu = [double[]]@($samples | ForEach-Object CpuPercent)
$workingSet = [double[]]@($samples | ForEach-Object WorkingSetMb)
$privateMemory = [double[]]@($samples | ForEach-Object PrivateMb)
$handles = [double[]]@($samples | ForEach-Object Handles)
$threads = [double[]]@($samples | ForEach-Object Threads)

[pscustomobject]@{
    ProcessId = $process.Id
    Samples = $samples.Count
    CpuAverage = [math]::Round(($cpu | Measure-Object -Average).Average, 2)
    CpuP95 = [math]::Round((Get-Percentile $cpu 0.95), 2)
    CpuMax = [math]::Round(($cpu | Measure-Object -Maximum).Maximum, 2)
    WorkingSetP95Mb = [math]::Round((Get-Percentile $workingSet 0.95), 2)
    PrivateMemoryP95Mb = [math]::Round((Get-Percentile $privateMemory 0.95), 2)
    HandlesP95 = [math]::Round((Get-Percentile $handles 0.95), 0)
    ThreadsP95 = [math]::Round((Get-Percentile $threads 0.95), 0)
    OutputPath = (Resolve-Path -LiteralPath $OutputPath).Path
} | Format-List
