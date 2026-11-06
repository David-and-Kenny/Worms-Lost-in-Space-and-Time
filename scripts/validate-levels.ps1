param(
    [ValidateSet('inspect', 'search', 'replay', 'batch')][string]$Operation = 'search',
    [string]$Level = 'all',
    [string]$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.6.8f1\Editor\Unity.com',
    [string]$ReplayFile,
    [string]$ReplayDirectory,
    [string]$ProposalDirectory,
    [string]$OutputDirectory,
    [ValidateRange(0, 2147483647)][int]$Seed = 1729,
    [ValidateRange(10, 240)][int]$FramesPerSecond = 60,
    [ValidateRange(1, 3600)][int]$SearchSeconds = 300,
    [ValidateRange(1, 100000)][int]$MaximumTrials = 10000,
    [ValidateRange(1, 600)][int]$TrialSeconds = 120,
    [ValidateRange(1, 64)][int]$MaximumJumps = 64,
    [ValidateRange(1, 1024)][int]$BeamWidth = 64
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$project = Join-Path $repoRoot 'Worms Lost in Space and Time(3.0)'
if (!$OutputDirectory) { $OutputDirectory = Join-Path $repoRoot ('TestResults/LevelValidation/' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff')) }
$output = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force -Path $output | Out-Null
foreach ($artifact in @('manifest.json', 'summary.json', 'summary.md', 'command-tests.xml')) {
    $old = Join-Path $output $artifact
    if (Test-Path -LiteralPath $old) { Remove-Item -LiteralPath $old }
}
$editor = (Resolve-Path -LiteralPath $UnityEditor).Path
if ($ReplayFile) {
    $ReplayFile = (Resolve-Path -LiteralPath $ReplayFile).Path
    $saved = Get-Content -LiteralPath $ReplayFile -Raw | ConvertFrom-Json
    if ($Level -eq 'all') { $Level = if ($saved.ScenePath) { $saved.ScenePath } else { $saved.LevelId } }
}
if ($ReplayDirectory) {
    $ReplayDirectory = (Resolve-Path -LiteralPath $ReplayDirectory).Path
    if ($Level -eq 'all') { $Level = 'certified' }
}
if ($ProposalDirectory) { $ProposalDirectory = (Resolve-Path -LiteralPath $ProposalDirectory).Path }
if ($Operation -eq 'replay' -and !$ReplayFile -and !$ReplayDirectory) { throw 'Replay requires -ReplayFile or -ReplayDirectory.' }
$request = @{ Version=1; Operation=$Operation; Level=$Level; OutputDirectory=$output; ReplayFile=$ReplayFile;
    ReplayDirectory=$ReplayDirectory; ProposalDirectory=$ProposalDirectory; Seed=$Seed; FramesPerSecond=$FramesPerSecond; SearchSeconds=$SearchSeconds; MaximumTrials=$MaximumTrials;
    MaximumJumps=$MaximumJumps; TrialSeconds=$TrialSeconds; BeamWidth=$BeamWidth; Visual=$false }
$requestPath = Join-Path $output 'request.json'
$request | ConvertTo-Json | Set-Content -LiteralPath $requestPath
$common = @('-batchmode', '-nographics', '-projectPath', $project, '-worms-validation-request', $requestPath)
function Invoke-Editor([string[]]$Arguments, [int]$TimeoutSeconds) {
    $start = [Diagnostics.ProcessStartInfo]::new()
    $start.FileName = $editor
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    foreach ($argument in $Arguments) { $start.ArgumentList.Add($argument) }
    $process = [Diagnostics.Process]::Start($start)
    try {
        if (!$process.WaitForExit($TimeoutSeconds * 1000)) {
            $process.Kill($true)
            $process.WaitForExit()
            throw "Unity exceeded the process watchdog; no validation verdict is available. See $output"
        }
        if ($process.ExitCode -ne 0) { throw "Unity execution failed ($($process.ExitCode)). See $output" }
    } finally { $process.Dispose() }
}
Invoke-Editor ($common + @('-quit', '-executeMethod', 'Worms.LevelValidation.ValidationCommands.Inspect', '-logFile', (Join-Path $output 'inspection.log'))) 180
if ($Operation -ne 'inspect') {
    $manifest = Get-Content -LiteralPath (Join-Path $output 'manifest.json') -Raw | ConvertFrom-Json
    $timeout = [int]($manifest.Levels.Count * ($SearchSeconds + 30) + 180)
    Invoke-Editor ($common + @('-runTests', '-testPlatform', 'PlayMode', '-testFilter', 'Worms.LevelValidation.LevelValidationCommandTests.ExecuteRequest', '-testResults', (Join-Path $output 'command-tests.xml'), '-logFile', (Join-Path $output 'validation.log'))) $timeout
    [xml]$tests = Get-Content -LiteralPath (Join-Path $output 'command-tests.xml') -Raw
    if ($tests.'test-run'.result -ne 'Passed' -or [int]$tests.'test-run'.passed -lt 1) { throw 'Validation command did not execute successfully.' }
}
$summary = Get-Content -LiteralPath (Join-Path $output 'summary.json') -Raw | ConvertFrom-Json
$summary.Levels | Select-Object LevelId,Status,Trials,Reason | Format-Table -Wrap | Out-Host
Write-Host "Reports: $output"
if (!$summary.Complete -or !$summary.Success) { exit 2 }
exit 0
