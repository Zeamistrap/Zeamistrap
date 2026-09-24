param(
    [Parameter(Mandatory = $true)]
    [string]$CsvPath,
    [string]$Application = 'RobloxPlayerBeta',
    [int]$ProcessId = 0,
    [double]$WarmupSeconds = 2
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $CsvPath -PathType Leaf)) {
    Write-Error "PresentMon CSV was not found: '$CsvPath'."
    exit 1
}

if ($WarmupSeconds -lt 0) {
    Write-Error 'WarmupSeconds must be zero or greater.'
    exit 1
}

$rows = @(Import-Csv -LiteralPath $CsvPath)
if ($rows.Count -eq 0) {
    Write-Error "PresentMon CSV contains no rows: '$CsvPath'."
    exit 1
}

function Get-Number([object]$Row, [string]$Name) {
    $value = $Row.$Name
    $result = 0.0
    if ([double]::TryParse(
            [string]$value,
            [Globalization.NumberStyles]::Float,
            [Globalization.CultureInfo]::InvariantCulture,
            [ref]$result)) {
        if ([double]::IsNaN($result) -or [double]::IsInfinity($result) -or
            $result -lt 0 -or $result -ge 1000000000000) {
            return $null
        }

        return $result
    }

    return $null
}

function Get-Percentile([double[]]$Values, [double]$Percentile) {
    $sorted = @($Values | Sort-Object)
    if ($sorted.Count -eq 0) {
        return $null
    }

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

$selected = @($rows | Where-Object {
    $applicationMatches = [string]::IsNullOrWhiteSpace($Application) -or
        ([string]$_.Application -like "*$Application*")
    $processMatches = $ProcessId -le 0 -or [string]$_.ProcessID -eq [string]$ProcessId
    $applicationMatches -and $processMatches
})

if ($selected.Count -eq 0) {
    Write-Error "No rows matched Application '$Application' and ProcessId $ProcessId."
    exit 1
}

$startTimes = @($selected | ForEach-Object { Get-Number $_ 'CPUStartTime' } |
    Where-Object { $null -ne $_ })
if ($startTimes.Count -gt 0 -and $WarmupSeconds -gt 0) {
    $cutoff = ($startTimes | Measure-Object -Minimum).Minimum + $WarmupSeconds
    $selected = @($selected | Where-Object {
        $start = Get-Number $_ 'CPUStartTime'
        $null -ne $start -and $start -ge $cutoff
    })
}

$frames = @($selected | ForEach-Object {
    $row = $_
    $frameTime = Get-Number $row 'FrameTime'
    if ($null -eq $frameTime -or $frameTime -le 0) {
        return
    }

    [pscustomobject]@{
        Application = [string]$row.Application
        ProcessId = [int]$row.ProcessID
        SwapChain = [string]$row.SwapChainAddress
        CpuStartTime = Get-Number $row 'CPUStartTime'
        FrameTimeMs = $frameTime
        FrameIntervalMs = $null
        CpuBusyMs = Get-Number $row 'CPUBusy'
        CpuWaitMs = Get-Number $row 'CPUWait'
        GpuLatencyMs = Get-Number $row 'GPULatency'
        GpuTimeMs = Get-Number $row 'GPUTime'
        GpuBusyMs = Get-Number $row 'GPUBusy'
        GpuWaitMs = Get-Number $row 'GPUWait'
        DisplayLatencyMs = Get-Number $row 'DisplayLatency'
        DisplayedTimeMs = Get-Number $row 'DisplayedTime'
        Fps = $null
    }
})

if ($frames.Count -eq 0) {
    Write-Error 'No valid positive FrameTime values remained after filtering.'
    exit 1
}

foreach ($group in ($frames | Group-Object -Property SwapChain)) {
    $ordered = @($group.Group |
        Where-Object { $null -ne $_.CpuStartTime } |
        Sort-Object CpuStartTime)
    $previousStart = $null

    foreach ($item in $ordered) {
        if ($null -ne $previousStart) {
            $interval = $item.CpuStartTime - $previousStart
            if ($interval -gt 0 -and $interval -lt 1000) {
                $item.FrameIntervalMs = $interval
            }
        }

        $previousStart = $item.CpuStartTime
    }
}

function New-Summary([string]$Name, [object[]]$Items) {
    $frameTimes = [double[]]@($Items | ForEach-Object FrameTimeMs)
    $intervals = [double[]]@($Items |
        Where-Object { $null -ne $_.FrameIntervalMs } |
        ForEach-Object FrameIntervalMs)
    $cpuBusy = [double[]]@($Items | Where-Object { $null -ne $_.CpuBusyMs } | ForEach-Object CpuBusyMs)
    $gpuTime = [double[]]@($Items | Where-Object { $null -ne $_.GpuTimeMs } | ForEach-Object GpuTimeMs)
    $displayLatency = [double[]]@($Items | Where-Object { $null -ne $_.DisplayLatencyMs } | ForEach-Object DisplayLatencyMs)
    $start = @($Items | Where-Object { $null -ne $_.CpuStartTime } | ForEach-Object CpuStartTime)
    $duration = if ($start.Count -gt 1) { $start[-1] - $start[0] } else { 0.0 }
    $averageInterval = if ($intervals.Count -gt 0) {
        ($intervals | Measure-Object -Average).Average
    } else {
        ($frameTimes | Measure-Object -Average).Average
    }

    [pscustomobject]@{
        Scope = $Name
        Frames = $items.Count
        DurationSeconds = [math]::Round($duration / 1000.0, 2)
        AverageFps = [math]::Round(1000.0 / $averageInterval, 2)
        Fps1PercentLow = if ($intervals.Count -gt 0) { [math]::Round(1000.0 / (Get-Percentile $intervals 0.99), 2) } else { $null }
        FrameIntervalP50Ms = if ($intervals.Count -gt 0) { [math]::Round((Get-Percentile $intervals 0.50), 3) } else { $null }
        FrameIntervalP95Ms = if ($intervals.Count -gt 0) { [math]::Round((Get-Percentile $intervals 0.95), 3) } else { $null }
        FrameIntervalP99Ms = if ($intervals.Count -gt 0) { [math]::Round((Get-Percentile $intervals 0.99), 3) } else { $null }
        CpuFrameTimeP50Ms = [math]::Round((Get-Percentile $frameTimes 0.50), 3)
        CpuFrameTimeP95Ms = [math]::Round((Get-Percentile $frameTimes 0.95), 3)
        CpuFrameTimeP99Ms = [math]::Round((Get-Percentile $frameTimes 0.99), 3)
        CpuFrameTimeMaxMs = [math]::Round(($frameTimes | Measure-Object -Maximum).Maximum, 3)
        CpuBusyP95Ms = if ($cpuBusy.Count -gt 0) { [math]::Round((Get-Percentile $cpuBusy 0.95), 3) } else { $null }
        GpuTimeP95Ms = if ($gpuTime.Count -gt 0) { [math]::Round((Get-Percentile $gpuTime 0.95), 3) } else { $null }
        DisplayLatencyP95Ms = if ($displayLatency.Count -gt 0) { [math]::Round((Get-Percentile $displayLatency 0.95), 3) } else { $null }
    }
}

$summaries = [System.Collections.Generic.List[object]]::new()
$summaries.Add((New-Summary 'AllSwapChains' $frames))

foreach ($group in ($frames | Group-Object -Property SwapChain | Sort-Object Count -Descending)) {
    $summaries.Add((New-Summary "SwapChain=$($group.Name)" ([object[]]$group.Group)))
}

$summaries | Format-Table -AutoSize
Write-Host "Rows matched: $($frames.Count) / $($rows.Count)"
Write-Host "Application: $Application; ProcessId filter: $ProcessId; Warmup: $WarmupSeconds seconds"
