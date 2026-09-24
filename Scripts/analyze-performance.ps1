param(
    [Parameter(Mandatory = $true)]
    [string[]]$LogPath
)

$inputFiles = @()

foreach ($path in $LogPath) {
    if (Test-Path -LiteralPath $path -PathType Container) {
        $inputFiles += @(Get-ChildItem -LiteralPath $path -Filter '*.log' -File)
    }
    elseif (Test-Path -LiteralPath $path -PathType Leaf) {
        $inputFiles += @(Get-Item -LiteralPath $path)
    }
    else {
        Write-Error "Log path was not found: '$path'."
        exit 1
    }
}

$records = @()

foreach ($file in $inputFiles) {
    Get-Content -LiteralPath $file.FullName -ErrorAction Stop | ForEach-Object {
        if ($_ -notmatch '\[Performance\]\s+(.+)$') {
            return
        }

        $values = @{}
        foreach ($part in $Matches[1] -split ';') {
            $pair = $part -split '=', 2
            if ($pair.Count -eq 2) {
                $values[$pair[0].Trim()] = $pair[1].Trim()
            }
        }

        if ($values.ContainsKey('stage') -and $values.ContainsKey('duration_ms')) {
            $duration = 0.0
            if ([double]::TryParse($values['duration_ms'], [Globalization.NumberStyles]::Float, [Globalization.CultureInfo]::InvariantCulture, [ref]$duration)) {
                $records += [pscustomobject]@{
                    Stage = $values['stage']
                    DurationMs = $duration
                    WorkingSetMb = if ($values.ContainsKey('working_set_mb')) { [double]$values['working_set_mb'] } else { $null }
                    PrivateMb = if ($values.ContainsKey('private_mb')) { [double]$values['private_mb'] } else { $null }
                    Handles = if ($values.ContainsKey('handles')) { [int]$values['handles'] } else { $null }
                    Threads = if ($values.ContainsKey('threads')) { [int]$values['threads'] } else { $null }
                }
            }
        }
    }
}

if ($records.Count -eq 0) {
    Write-Error "No [Performance] records were found in '$LogPath'."
    exit 1
}

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

$summary = $records |
    Group-Object Stage |
    ForEach-Object {
        $durations = @($_.Group | ForEach-Object DurationMs)
        [pscustomobject]@{
            Stage = $_.Name
            Samples = $durations.Count
            MinMs = [math]::Round(($durations | Measure-Object -Minimum).Minimum, 2)
            AverageMs = [math]::Round(($durations | Measure-Object -Average).Average, 2)
            P50Ms = [math]::Round((Get-Percentile $durations 0.50), 2)
            P95Ms = [math]::Round((Get-Percentile $durations 0.95), 2)
            MaxMs = [math]::Round(($durations | Measure-Object -Maximum).Maximum, 2)
            LastWorkingSetMb = $_.Group[-1].WorkingSetMb
            LastPrivateMb = $_.Group[-1].PrivateMb
            LastHandles = $_.Group[-1].Handles
            LastThreads = $_.Group[-1].Threads
        }
    } |
    Sort-Object Stage

$summary | Format-Table -AutoSize
