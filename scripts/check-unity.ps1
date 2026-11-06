param(
    [Parameter(Mandatory = $true)][string]$UnityEditor,
    [ValidateSet('Import', 'Scenes', 'PlayMode', 'BuildWindows', 'BuildAndroid')][string]$Check = 'Scenes'
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$project = Join-Path $repoRoot 'Worms Lost in Space and Time(3.0)'
$logFolder = Join-Path $repoRoot 'Logs'
New-Item -ItemType Directory -Force -Path $logFolder | Out-Null
$editorPath = (Resolve-Path -LiteralPath $UnityEditor).Path
$arguments = @('-batchmode', '-nographics', '-quit', '-accept-apiupdate', '-projectPath', $project, '-logFile', (Join-Path $logFolder "$Check.log"))
if ($Check -eq 'Scenes') { $arguments += @('-executeMethod', 'DevelopmentChecks.ValidateScenes') }
if ($Check -eq 'BuildWindows') { $arguments += @('-executeMethod', 'DevelopmentChecks.BuildWindows') }
if ($Check -eq 'BuildAndroid') { $arguments += @('-executeMethod', 'DevelopmentChecks.BuildAndroid') }
if ($Check -eq 'PlayMode') {
    $arguments = $arguments | Where-Object { $_ -ne '-quit' }
    $testFolder = Join-Path $repoRoot 'TestResults'
    New-Item -ItemType Directory -Force -Path $testFolder | Out-Null
    $arguments += @('-runTests', '-testPlatform', 'PlayMode', '-testResults', (Join-Path $testFolder 'PlayMode.xml'))
}
& $editorPath @arguments
if ($LASTEXITCODE -ne 0) { throw "Unity $Check failed ($LASTEXITCODE). See Logs/$Check.log." }
Write-Output "Unity $Check passed. See Logs/$Check.log."
