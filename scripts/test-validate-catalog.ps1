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
    $FramesPerSecond=60,$TrialSeconds=120,$BeamWidth=256,$MaximumTrials=10000,$ReplayDirectory,$ProposalDirectory,$ReplayFile)
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
    if ($env:WORMS_CATALOG_TEST_CASE -eq 'supplemental') { throw 'An already verified supplemental route must not be searched again.' }
    if ($ReplayFile) {
        if ($Level -ne 'level-01' -or $FramesPerSecond -ne 30 -or $env:WORMS_CATALOG_TEST_CASE -ne 'analysis') { throw 'Wrong analysis selection.' }
    } elseif ($BeamWidth -eq 16) {
        if ($Level -ne 'level-02' -or $FramesPerSecond -ne 30 -or $TrialSeconds -ne 60 -or $SearchSeconds -ne 600 -or $MaximumTrials -ne 50000 -or $env:WORMS_CATALOG_TEST_CASE -ne 'focused') { throw 'Wrong focused selection/profile.' }
    } elseif ($Level -ne 'level-02' -or $FramesPerSecond -ne 30 -or $TrialSeconds -ne 30 -or $BeamWidth -ne 1024) { throw 'Wrong fallback selection/profile.' }
    $rows=@((Row $Level 'VerifiedReachable'))
    if ($env:WORMS_CATALOG_TEST_CASE -eq 'focused' -and $BeamWidth -eq 1024) { $rows=@((Row $Level 'Inconclusive')) }
} elseif ($ReplayDirectory) {
    $rows=@(Get-ChildItem -LiteralPath $ReplayDirectory -Filter '*.replay.json' | ForEach-Object { Row ((Get-Content $_.FullName -Raw | ConvertFrom-Json).LevelId) 'VerifiedReachable' })
} else { $rows=@((Row 'level-01' 'VerifiedReachable'),(Row 'level-02' 'Inconclusive'),(Row 'level-20' 'InvalidContent')) }
if ($env:WORMS_CATALOG_TEST_CASE -eq 'analysis' -and $Operation -eq 'batch' -and !$ReplayDirectory) { $rows[0].AnalysisComplete=$false }
if ($env:WORMS_CATALOG_TEST_CASE -eq 'replay-failure' -and $ReplayDirectory) { $rows[0].Status='ReplayFailed' }
if ($env:WORMS_CATALOG_TEST_CASE -eq 'unstable') { $rows[0].Status='RunnerUnstable' }
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
foreach($row in $rows | Where-Object HasReplay) { $row.Replay | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $OutputDirectory ($row.LevelId+'.replay.json')) }
@{Version=1;Complete=($env:WORMS_CATALOG_TEST_CASE -ne 'partial');RequestedLevels=$rows.Count;Success=(@($rows | Where-Object {$_.Status -ne 'VerifiedReachable'}).Count -eq 0);Levels=$rows} |
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

    $env:WORMS_CATALOG_TEST_CASE = 'analysis'
    $analysisOutput = Join-Path $fixtureRoot 'Analysis'
    & $shellPath -NoProfile -File $runner -OutputDirectory $analysisOutput *> (Join-Path $fixtureRoot 'analysis.log')
    if ($LASTEXITCODE -ne 2) { throw 'Analysis scenario did not preserve the content blocker.' }
    $analyzed = Get-Content -LiteralPath (Join-Path $analysisOutput 'summary.json') -Raw | ConvertFrom-Json
    if (!$analyzed.LevelSources.'level-01'.Contains('Analysis-level-01')) { throw 'Analysis evidence was not selected.' }
    Write-Host 'PASS: incomplete timing analysis gets a separate budget'

    $env:WORMS_CATALOG_TEST_CASE = 'focused'
    $focusedOutput = Join-Path $fixtureRoot 'Focused'
    & $shellPath -NoProfile -File $runner -OutputDirectory $focusedOutput *> (Join-Path $fixtureRoot 'focused.log')
    if ($LASTEXITCODE -ne 2) { throw 'Focused scenario did not preserve the content blocker.' }
    $focused = Get-Content -LiteralPath (Join-Path $focusedOutput 'summary.json') -Raw | ConvertFrom-Json
    if (!$focused.LevelSources.'level-02'.Contains('Focused-level-02')) { throw 'Focused fallback evidence was not selected.' }
    Write-Host 'PASS: narrow-beam fallback spends more angles on each arrival'

    $env:WORMS_CATALOG_TEST_CASE = 'normal'
    & $shellPath -NoProfile -File $runner -InitialRun (Join-Path $output 'Initial') -OutputDirectory (Join-Path $fixtureRoot 'Resume') *> (Join-Path $fixtureRoot 'resume.log')
    if ($LASTEXITCODE -ne 2) { throw 'Same-source complete resume failed.' }
    Write-Host 'PASS: same-source resume accepted'

    $env:WORMS_CATALOG_TEST_CASE = 'supplemental'
    $supplementalOutput = Join-Path $fixtureRoot 'Supplemental'
    & $shellPath -NoProfile -File $runner -InitialRun (Join-Path $output 'Initial') -AdditionalRuns (Join-Path $output 'Wide-level-02') -OutputDirectory $supplementalOutput *> (Join-Path $fixtureRoot 'supplemental.log')
    if ($LASTEXITCODE -ne 2) { throw 'Same-source supplemental evidence failed.' }
    $supplemental = Get-Content -LiteralPath (Join-Path $supplementalOutput 'summary.json') -Raw | ConvertFrom-Json
    if (($supplemental.Levels | Where-Object LevelId -eq 'level-02').Status -ne 'VerifiedReachable') { throw 'Supplemental proof was lost.' }
    Write-Host 'PASS: same-source supplemental proof skips redundant search and gets strict replay'

    $wrongSource = Join-Path $fixtureRoot 'WrongSource'
    New-Item -ItemType Directory -Path $wrongSource | Out-Null
    $wrong = Get-Content -LiteralPath (Join-Path $output 'Wide-level-02/summary.json') -Raw | ConvertFrom-Json
    $wrong.Levels[0].Fingerprint = 'changed'
    $wrong | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $wrongSource 'summary.json')
    & $shellPath -NoProfile -File $runner -InitialRun (Join-Path $output 'Initial') -AdditionalRuns $wrongSource -OutputDirectory (Join-Path $fixtureRoot 'WrongSupplement') *> (Join-Path $fixtureRoot 'wrong-supplement.log')
    if ($LASTEXITCODE -eq 0 -or $LASTEXITCODE -eq 2) { throw 'Changed-source supplemental evidence accepted.' }
    Write-Host 'PASS: changed-source supplemental proof rejected'

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

    foreach ($scenario in @('partial','unstable','replay-failure')) {
        $env:WORMS_CATALOG_TEST_CASE = $scenario
        & $shellPath -NoProfile -File $runner -OutputDirectory (Join-Path $fixtureRoot $scenario) *> (Join-Path $fixtureRoot ($scenario+'.log'))
        if ($LASTEXITCODE -eq 0 -or $LASTEXITCODE -eq 2) { throw "Invalid evidence accepted: $scenario" }
        Write-Host "PASS: $scenario rejected"
    }
} finally {
    $env:WORMS_CATALOG_TEST_CASE = $previousCase
    $resolvedFixture = [IO.Path]::GetFullPath($fixtureRoot)
    $temporaryRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    if (!$resolvedFixture.StartsWith($temporaryRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe fixture cleanup path.' }
    if (Test-Path -LiteralPath $resolvedFixture) { Remove-Item -LiteralPath $resolvedFixture -Recurse -Force }
}
