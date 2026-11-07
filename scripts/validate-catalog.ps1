param(
    [string]$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.6.8f1\Editor\Unity.com',
    [string]$OutputDirectory,
    [string]$InitialRun,
    [ValidateRange(10, 240)][int]$FramesPerSecond = 30,
    [ValidateRange(1, 3600)][int]$SearchSeconds = 300,
    [ValidateRange(1, 3600)][int]$FallbackSeconds = 300,
    [ValidateRange(1, 3600)][int]$FocusedSeconds = 300
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
if (!$OutputDirectory) { $OutputDirectory = Join-Path $repoRoot ('TestResults/Catalog/' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff')) }
$output = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force -Path $output | Out-Null
$certificates = Join-Path $output 'Certificates'
if ((Test-Path -LiteralPath $certificates) -and (Get-ChildItem -LiteralPath $certificates -Filter '*.replay.json' -File)) {
    throw 'Choose a fresh output directory; an earlier certificate set must not leak into this run.'
}
New-Item -ItemType Directory -Force -Path $certificates | Out-Null
$sources = [Collections.Generic.List[string]]::new()

function Read-CompleteRun([string]$Directory) {
    $file = Join-Path $Directory 'summary.json'
    $summary = Get-Content -LiteralPath $file -Raw | ConvertFrom-Json
    if (!$summary.Complete -or $summary.Levels.Count -ne $summary.RequestedLevels) { throw "Incomplete run: $Directory" }
    if ($summary.Levels | Where-Object { $_.Status -eq 'ExecutionError' -or $_.Status -eq 'RunnerUnstable' }) {
        throw "Execution/repeatability failure requires investigation: $Directory"
    }
    $sources.Add($file)
    foreach ($level in $summary.Levels) {
        if ($level.Status -eq 'VerifiedReachable' -and $level.HasReplay) {
            Copy-Item -LiteralPath (Join-Path $Directory ($level.LevelId + '.replay.json')) -Destination $certificates -Force
        }
    }
    return $summary
}

function Invoke-LevelRun([hashtable]$Options) {
    & (Join-Path $PSScriptRoot 'validate-levels.ps1') -UnityEditor $UnityEditor @Options | Out-Host
    if ($LASTEXITCODE -notin @(0, 2)) { throw "Unity validation failed with exit code $LASTEXITCODE." }
    return Read-CompleteRun $Options.OutputDirectory
}

if ($InitialRun) {
    # A fresh structural inspection establishes current fingerprints before accepting a resumed run.
    $inspection = Invoke-LevelRun @{ Operation='inspect'; Level='all'; OutputDirectory=(Join-Path $output 'Inspection') }
    $initial = Read-CompleteRun ([IO.Path]::GetFullPath($InitialRun))
    foreach ($level in $initial.Levels) {
        $current = $inspection.Levels | Where-Object { $_.LevelId -eq $level.LevelId }
        if (!$current -or $current.Fingerprint -ne $level.Fingerprint) { throw "Cannot resume changed sources for $($level.LevelId)." }
    }
    if ($initial.Levels.Count -ne $inspection.Levels.Count) { throw 'Cannot resume a partial catalog selection.' }
} else {
    $options = @{ Operation='batch'; Level='all'; SearchSeconds=$SearchSeconds; TimingRadiusTicks=2; FramesPerSecond=$FramesPerSecond;
        OutputDirectory=(Join-Path $output 'Initial') }
    $maintained = Join-Path $repoRoot 'ValidationRoutes'
    if (Test-Path -LiteralPath $maintained) { $options.ProposalDirectory = $maintained }
    $initial = Invoke-LevelRun $options
}

foreach ($level in $initial.Levels) {
    if ($level.Status -ne 'Inconclusive') { continue }
    $fallback = Invoke-LevelRun @{ Operation='search'; Level=$level.LevelId; SearchSeconds=$FallbackSeconds;
        FramesPerSecond=30; TrialSeconds=30; BeamWidth=1024; TimingRadiusTicks=2;
        OutputDirectory=(Join-Path $output ('Wide-' + $level.LevelId)) }
    if ($fallback.Levels[0].Status -eq 'Inconclusive') {
        # A small beam spends more angles on each retained arrival instead of spreading effort thinly.
        $focused = Invoke-LevelRun @{ Operation='search'; Level=$level.LevelId; SearchSeconds=$FocusedSeconds;
            FramesPerSecond=30; TrialSeconds=60; BeamWidth=16; TimingRadiusTicks=2;
            OutputDirectory=(Join-Path $output ('Focused-' + $level.LevelId)) }
    }
}

# Complete sensitivity evidence separately from route discovery when the first budget ran out.
foreach ($certificate in Get-ChildItem -LiteralPath $certificates -Filter '*.replay.json' -File) {
    $id = (Get-Content -LiteralPath $certificate.FullName -Raw | ConvertFrom-Json).LevelId
    $runs = foreach ($source in $sources) {
        (Get-Content -LiteralPath $source -Raw | ConvertFrom-Json).Levels |
            Where-Object { $_.LevelId -eq $id -and $_.Status -eq 'VerifiedReachable' }
    }
    if ($runs | Where-Object { $_.AnalysisComplete }) { continue }
    $analysis = Invoke-LevelRun @{ Operation='search'; Level=$id; ReplayFile=$certificate.FullName;
        SearchSeconds=$FallbackSeconds; TimingRadiusTicks=2; FramesPerSecond=$FramesPerSecond;
        OutputDirectory=(Join-Path $output ('Analysis-' + $id)) }
}

$arguments = @((Join-Path $PSScriptRoot 'report-level-validation.mjs'))
foreach ($source in $sources) { $arguments += @('--summary', $source) }
$arguments += @('--output', (Join-Path $output 'report.md'), '--csv', (Join-Path $output 'report.csv'), '--json', (Join-Path $output 'summary.json'))
& node @arguments
if ($LASTEXITCODE -ne 0) { throw 'Report aggregation failed; sources may differ.' }
$final = Get-Content -LiteralPath (Join-Path $output 'summary.json') -Raw | ConvertFrom-Json
foreach ($level in $final.Levels | Where-Object { $_.Status -eq 'VerifiedReachable' }) {
    $source = $final.LevelSources.($level.LevelId)
    Copy-Item -LiteralPath (Join-Path (Split-Path $source -Parent) ($level.LevelId + '.replay.json')) -Destination $certificates -Force
}
if (Get-ChildItem -LiteralPath $certificates -Filter '*.replay.json' -File) {
    $strict = Invoke-LevelRun @{ Operation='batch'; ReplayDirectory=$certificates; OutputDirectory=(Join-Path $output 'Replay') }
    if (!$strict.Success) { throw 'Certified routes failed strict replay.' }
}
Write-Host "Catalog report: $(Join-Path $output 'report.md')"
if (!$final.Success) { exit 2 }
exit 0
