# Exercises the validation runner's report/exit contract without launching Unity.
$ErrorActionPreference = 'Stop'
$fixtureRoot = Join-Path ([IO.Path]::GetTempPath()) ('worms-runner-tests-' + [Guid]::NewGuid().ToString('N'))
$shellPath = (Get-Process -Id $PID).Path
$cases = @(
    @{ Name = 'Passing report'; ExitCode = 0; Xml = '<test-run result="Passed" total="2" passed="2" failed="0" />'; Pass = $true },
    @{ Name = 'Android preprocessor target'; Check = 'BuildAndroid'; ExitCode = 0; Xml = $null; Pass = $true },
    @{ Name = 'Editor failure'; ExitCode = 9; Xml = $null; Pass = $false },
    @{ Name = 'Missing editor'; MissingEditor = $true; ExitCode = 0; Xml = $null; Pass = $false },
    @{ Name = 'Missing report'; ExitCode = 0; Xml = $null; Pass = $false },
    @{ Name = 'Empty suite'; ExitCode = 0; Xml = '<test-run result="Passed" total="0" passed="0" failed="0" />'; Pass = $false },
    @{ Name = 'Failed tests'; ExitCode = 0; Xml = '<test-run result="Failed" total="2" passed="1" failed="1" />'; Pass = $false },
    @{ Name = 'Skipped-only suite'; ExitCode = 0; Xml = '<test-run result="Passed" total="2" passed="0" failed="0" skipped="2" />'; Pass = $false }
)
try {
    $fixtureScripts = Join-Path $fixtureRoot 'scripts'
    $fixtureResults = Join-Path $fixtureRoot 'TestResults'
    New-Item -ItemType Directory -Path $fixtureScripts, $fixtureResults -Force | Out-Null
    $runner = Join-Path $fixtureScripts 'check-unity.ps1'
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'check-unity.ps1') -Destination $runner
    $fakeEditor = Join-Path $fixtureRoot 'fake-editor.ps1'
    @'
$scenario = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'scenario.json') -Raw | ConvertFrom-Json
ConvertTo-Json -InputObject @($args) | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'invocation.json')
$reportIndex = [Array]::IndexOf($args, '-testResults')
if ($null -ne $scenario.Xml) {
    Set-Content -LiteralPath $args[$reportIndex + 1] -Value $scenario.Xml
}
$global:LASTEXITCODE = [int]$scenario.ExitCode
'@ | Set-Content -LiteralPath $fakeEditor
    foreach ($case in $cases) {
        $case | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $fixtureRoot 'scenario.json')
        $summary = Join-Path $fixtureResults 'validation-summary.json'
        # Both old reports must be rejected when the current invocation fails.
        '{"Check":"Old successful run"}' | Set-Content -LiteralPath $summary
        '<test-run result="Passed" total="1" passed="1" failed="0" />' |
            Set-Content -LiteralPath (Join-Path $fixtureResults 'EditMode.xml')
        $editorForCase = if ($case.MissingEditor) { Join-Path $fixtureRoot 'missing-editor.ps1' } else { $fakeEditor }
        $check = if ($case.Check) { $case.Check } else { 'EditMode' }
        $output = & $shellPath -NoProfile -File $runner -UnityEditor $editorForCase -Check $check 2>&1
        $succeeded = $LASTEXITCODE -eq 0
        if ($succeeded -ne $case.Pass) { throw "$($case.Name): unexpected exit. $output" }
        if ($case.Pass) {
            $result = Get-Content -LiteralPath $summary -Raw | ConvertFrom-Json
            if ($result.Check -ne $check -or ($check -eq 'EditMode' -and $result.Tests.Passed -ne 2)) {
                throw 'Passing invocation did not publish its own results.'
            }
            if ($check -eq 'BuildAndroid') {
                $invocation = @(Get-Content -LiteralPath (Join-Path $fixtureRoot 'invocation.json') -Raw | ConvertFrom-Json)
                $targetIndex = [Array]::IndexOf($invocation, '-buildTarget')
                if ($targetIndex -lt 0 -or $invocation[$targetIndex + 1] -ne 'Android') {
                    throw 'Android build did not select its editor target before SDK preprocessing.'
                }
            }
        } elseif (Test-Path -LiteralPath $summary) {
            throw "$($case.Name): stale success summary survived a failed invocation."
        }
        Write-Host "PASS: $($case.Name)"
    }
} finally {
    # The only recursive removal is the isolated directory created above.
    if (Test-Path -LiteralPath $fixtureRoot) {
        $resolvedFixture = (Resolve-Path -LiteralPath $fixtureRoot).Path
        $resolvedTemp = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([char[]]@('\', '/')) + [IO.Path]::DirectorySeparatorChar
        if (!$resolvedFixture.StartsWith($resolvedTemp, [StringComparison]::OrdinalIgnoreCase)) {
            throw 'Runner fixture escaped the temporary directory.'
        }
        Remove-Item -LiteralPath $resolvedFixture -Recurse -Force
    }
}
# The final fixture intentionally fails; do not leak its native exit code to CI.
exit 0
