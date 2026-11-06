param(
    [Parameter(Mandatory = $true)][string]$UnityEditor,
    [ValidateSet('Import', 'Scenes', 'EditMode', 'PlayMode', 'BuildWindows', 'BuildAndroid', 'All')]
    [string]$Check = 'Scenes',
    [string]$TestFilter
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$project = Join-Path $repoRoot 'Worms Lost in Space and Time(3.0)'
$logFolder = Join-Path $repoRoot 'Logs'
$testFolder = Join-Path $repoRoot 'TestResults'
New-Item -ItemType Directory -Force -Path $logFolder, $testFolder | Out-Null
$summaryPath = Join-Path $testFolder 'validation-summary.json'
# A summary certifies this invocation only; a failed run must not leave an old success.
if (Test-Path -LiteralPath $summaryPath) { Remove-Item -LiteralPath $summaryPath }
$editorPath = (Resolve-Path -LiteralPath $UnityEditor).Path

function Read-TestResult([string]$ResultPath) {
    if (!(Test-Path -LiteralPath $ResultPath)) { throw "Unity did not produce a test report: $ResultPath" }
    [xml]$report = Get-Content -LiteralPath $ResultPath -Raw
    $run = $report.'test-run'
    if (!$run -or [int]$run.total -lt 1 -or ([int]$run.passed + [int]$run.failed) -lt 1) {
        throw "No tests executed: $ResultPath"
    }
    if ($run.result -ne 'Passed' -or [int]$run.failed -gt 0) {
        $failures = $report.SelectNodes('//test-case/failure/message') | ForEach-Object InnerText
        throw "Tests failed: $($failures -join [Environment]::NewLine). See $ResultPath"
    }
    return [pscustomobject]@{ Total = [int]$run.total; Passed = [int]$run.passed; Failed = [int]$run.failed }
}

function Invoke-UnityCheck([string]$Name) {
    $logPath = Join-Path $logFolder "$Name.log"
    $arguments = @('-batchmode', '-nographics', '-accept-apiupdate', '-projectPath', $project, '-logFile', $logPath)
    $isTest = $Name -in @('EditMode', 'PlayMode')
    if ($isTest) {
        $resultPath = Join-Path $testFolder "$Name.xml"
        if (Test-Path -LiteralPath $resultPath) { Remove-Item -LiteralPath $resultPath }
        $arguments += @('-runTests', '-testPlatform', $Name, '-testResults', $resultPath)
        if ($TestFilter) { $arguments += @('-testFilter', $TestFilter) }
    } else {
        $arguments += '-quit'
        $methods = @{ Scenes = 'DevelopmentChecks.ValidateScenes'; BuildWindows = 'DevelopmentChecks.BuildWindows'; BuildAndroid = 'DevelopmentChecks.BuildAndroid' }
        if ($methods.ContainsKey($Name)) { $arguments += @('-executeMethod', $methods[$Name]) }
    }
    & $editorPath @arguments | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "Unity $Name failed ($LASTEXITCODE). See $logPath." }
    $tests = if ($isTest) { Read-TestResult $resultPath } else { $null }
    Write-Host "Unity $Name passed. See $logPath."
    return [pscustomobject]@{ Check = $Name; Tests = $tests; Log = $logPath }
}

$checks = if ($Check -eq 'All') { @('Scenes', 'EditMode', 'PlayMode', 'BuildWindows', 'BuildAndroid') } else { @($Check) }
if ($Check -eq 'All') {
    & node (Join-Path $PSScriptRoot 'validate-repository.mjs')
    if ($LASTEXITCODE -ne 0) { throw 'Repository validation failed.' }
}
$results = foreach ($name in $checks) { Invoke-UnityCheck $name }
$results | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $summaryPath
