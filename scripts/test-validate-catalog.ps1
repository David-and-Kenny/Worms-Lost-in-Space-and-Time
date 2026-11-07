# Exercises portfolio orchestration with a fake backend; no Unity, SDKs or network.
$ErrorActionPreference = 'Stop'
$fixtureRoot = Join-Path ([IO.Path]::GetTempPath()) ('worms-catalog-' + [Guid]::NewGuid().ToString('N'))
$shellPath = (Get-Process -Id $PID).Path
$previousCase = $env:WORMS_CATALOG_TEST_CASE
try {
    $fixtureScripts = Join-Path $fixtureRoot 'scripts'
    New-Item -ItemType Directory -Force -Path $fixtureScripts | Out-Null
    foreach ($file in @('validate-catalog.ps1','report-level-validation.mjs')) {
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot $file) -Destination $fixtureScripts
    }
    @'
param($UnityEditor,$Operation,$Level='all',$OutputDirectory,$SearchSeconds,$TimingRadiusTicks,
    $FramesPerSecond=60,$TrialSeconds=120,$BeamWidth=256,$ReplayDirectory,$ProposalDirectory,$ReplayFile)
if ($env:WORMS_CATALOG_TEST_CASE -eq 'infrastructure') { exit 7 }
function Row($id,$status) {
    $fingerprint='current-'+$id
    if ($Operation -eq 'inspect' -and $env:WORMS_CATALOG_TEST_CASE -eq 'stale') { $fingerprint='changed' }
    return [pscustomobject]@{ LevelId=$id; Status=$status; Fingerprint=$fingerprint;
        HasReplay=($status -eq 'VerifiedReachable'); AnalysisComplete=$true; CompletionSeconds=5.2;
        PerturbationTrials=0; PerturbationSuccesses=0; Reason='Fixture outcome'; TimingWindows=@();
        Replay=@{Version=2;LevelId=$id;FramesPerSecond=60;Jumps=@()} }
}
if ($Operation -eq 'inspect') { $rows=@((Row 'level-01' 'Inconclusive'),(Row 'level-02' 'Inconclusive'),(Row 'level-20' 'InvalidContent')) }
elseif ($Operation -eq 'search') {
    if ($Level -ne 'level-02' -or $FramesPerSecond -ne 30 -or $TrialSeconds -ne 30 -or $BeamWidth -ne 1024) { throw 'Wrong fallback selection/profile.' }
    $rows=@((Row $Level 'VerifiedReachable'))
} elseif ($ReplayDirectory) {
    $rows=@(Get-ChildItem -LiteralPath $ReplayDirectory -Filter '*.replay.json' | ForEach-Object { Row ((Get-Content $_.FullName -Raw | ConvertFrom-Json).LevelId) 'VerifiedReachable' })
} else { $rows=@((Row 'level-01' 'VerifiedReachable'),(Row 'level-02' 'Inconclusive'),(Row 'level-20' 'InvalidContent')) }
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
foreach($row in $rows | Where-Object HasReplay) { $row.Replay | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $OutputDirectory ($row.LevelId+'.replay.json')) }
@{Version=1;Complete=$true;RequestedLevels=$rows.Count;Success=(@($rows | Where-Object {$_.Status -ne 'VerifiedReachable'}).Count -eq 0);Levels=$rows} |
    ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'summary.json')
if ($rows | Where-Object {$_.Status -ne 'VerifiedReachable'}) { exit 2 }
exit 0
'@ | Set-Content -LiteralPath (Join-Path $fixtureScripts 'validate-levels.ps1')

    $runner = Join-Path $fixtureScripts 'validate-catalog.ps1'
    $output = Join-Path $fixtureRoot 'Success'
    $env:WORMS_CATALOG_TEST_CASE = 'normal'
    & $shellPath -NoProfile -File $runner -OutputDirectory $output *> (Join-Path $fixtureRoot 'success.log')
    if ($LASTEXITCODE -ne 2) { throw 'A missing finish must retain logical exit 2.' }
    $summary = Get-Content -LiteralPath (Join-Path $output 'summary.json') -Raw | ConvertFrom-Json
    if (!$summary.Complete -or $summary.RequestedLevels -ne 3 -or $summary.Success) { throw 'Wrong combined completion/status.' }
    if (($summary.Levels | Where-Object LevelId -eq 'level-02').Status -ne 'VerifiedReachable') { throw 'Fallback did not replace the unresolved result.' }
    if ((Get-ChildItem -LiteralPath (Join-Path $output 'Certificates') -Filter '*.replay.json').Count -ne 2) { throw 'Wrong certificate selection.' }
    if (!(Test-Path -LiteralPath (Join-Path $output 'Replay/summary.json'))) { throw 'Strict replay was not executed.' }
    Write-Host 'PASS: fallback profile, merged evidence, certificate selection, content blocker and strict replay'

    & $shellPath -NoProfile -File $runner -OutputDirectory $output *> (Join-Path $fixtureRoot 'reuse.log')
    if ($LASTEXITCODE -eq 0 -or $LASTEXITCODE -eq 2) { throw 'Reused certificate output was accepted.' }
    Write-Host 'PASS: stale output rejected'

    $env:WORMS_CATALOG_TEST_CASE = 'stale'
    & $shellPath -NoProfile -File $runner -InitialRun (Join-Path $output 'Initial') -OutputDirectory (Join-Path $fixtureRoot 'Stale') *> (Join-Path $fixtureRoot 'stale.log')
    if ($LASTEXITCODE -eq 0 -or $LASTEXITCODE -eq 2) { throw 'Changed-source resume was accepted.' }
    Write-Host 'PASS: changed-source resume rejected'

    $env:WORMS_CATALOG_TEST_CASE = 'infrastructure'
    & $shellPath -NoProfile -File $runner -OutputDirectory (Join-Path $fixtureRoot 'Failure') *> (Join-Path $fixtureRoot 'failure.log')
    if ($LASTEXITCODE -eq 0 -or $LASTEXITCODE -eq 2) { throw 'Execution failure was converted into a logical outcome.' }
    Write-Host 'PASS: infrastructure failure propagated'
} finally {
    $env:WORMS_CATALOG_TEST_CASE = $previousCase
    $resolvedFixture = [IO.Path]::GetFullPath($fixtureRoot)
    $temporaryRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    if (!$resolvedFixture.StartsWith($temporaryRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe fixture cleanup path.' }
    if (Test-Path -LiteralPath $resolvedFixture) { Remove-Item -LiteralPath $resolvedFixture -Recurse -Force }
}
