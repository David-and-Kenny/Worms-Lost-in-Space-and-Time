param([Parameter(Mandatory = $true)][string]$UnityEditor)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$editorPath = (Resolve-Path -LiteralPath $UnityEditor).Path
$mergePath = Join-Path (Split-Path $editorPath -Parent) 'Data/Tools/UnityYAMLMerge.exe'
if (!(Test-Path -LiteralPath $mergePath)) { throw "UnityYAMLMerge not found: $mergePath" }
Push-Location $repoRoot
try {
    git config --local pull.ff only
    git config --local fetch.prune true
    git config --local merge.unityyamlmerge.name 'Unity Smart Merge'
    $driver = '"' + $mergePath.Replace('\', '/') + '" merge -p %O %B %A %A'
    git config --local merge.unityyamlmerge.driver $driver
    git config --local merge.unityyamlmerge.recursive binary
    git lfs install --local
    if ($LASTEXITCODE -ne 0) { throw 'Git LFS setup failed.' }
    Write-Output 'Configured repository-local Unity Smart Merge, fast-forward pulls, pruning, and Git LFS.'
} finally { Pop-Location }
