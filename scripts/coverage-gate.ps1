<#
.SYNOPSIS
    Enforces the repository coverage policy.

.DESCRIPTION
    Policy is 100% line and branch coverage, no exclusions. This gate is a
    RATCHET towards that: it fails when coverage drops below the floor recorded
    in coverage-policy.json, so the numbers can only go up. Raise the floors as
    tests land; never lower them.

    Reads the cobertura report produced by:
        dotnet test --collect:"XPlat Code Coverage" --settings coverage.runsettings

    coverage.runsettings strips compiler-generated code before it is counted --
    the GeneratedRegex source generator's output and Avalonia's XamlClosure
    types. That is not a policy exclusion; it is code nobody wrote. Leaving it
    in inflated the denominator by 28 classes and made 100% unreachable by
    construction.

.PARAMETER ResultsDirectory
    Where to look for coverage.cobertura.xml. Defaults to TestResults.
#>
[CmdletBinding()]
param(
    [string]$ResultsDirectory = 'TestResults'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot

$policyPath = Join-Path $repoRoot 'coverage-policy.json'
if (-not (Test-Path $policyPath)) {
    Write-Error "coverage-policy.json not found at $policyPath"
    exit 1
}
$policy = Get-Content $policyPath -Raw | ConvertFrom-Json

# Newest report wins: a stale one from an earlier run would gate on the wrong
# numbers and pass a build that should have failed.
$report = Get-ChildItem -Path (Join-Path $repoRoot $ResultsDirectory) -Recurse -Filter 'coverage.cobertura.xml' -ErrorAction SilentlyContinue |
          Sort-Object LastWriteTime -Descending |
          Select-Object -First 1

if (-not $report) {
    Write-Error @"
No coverage.cobertura.xml under '$ResultsDirectory'. Run:
  dotnet test --collect:"XPlat Code Coverage" --settings coverage.runsettings --results-directory $ResultsDirectory
"@
    exit 1
}

[xml]$xml = Get-Content $report.FullName -Raw
$root = $xml.coverage

$linesValid    = [int]$root.'lines-valid'
$linesCovered  = [int]$root.'lines-covered'
$branchesValid = [int]$root.'branches-valid'
$branchesCov   = [int]$root.'branches-covered'

if ($linesValid -eq 0) {
    Write-Error 'The report contains no lines. Refusing to pass a gate on nothing.'
    exit 1
}

function Get-Pct([int]$hit, [int]$total) {
    if ($total -eq 0) { return 100.0 }
    return ($hit / $total) * 100.0
}

$actual = @{
    line   = Get-Pct $linesCovered $linesValid
    branch = Get-Pct $branchesCov  $branchesValid
}

# ------------------------------------------------------------------- report
Write-Host ''
Write-Host 'Coverage gate'
Write-Host '-------------'
Write-Host ("  report   : {0}" -f $report.FullName.Substring($repoRoot.Length + 1))
foreach ($metric in @('line', 'branch')) {
    $hit   = if ($metric -eq 'line') { $linesCovered } else { $branchesCov }
    $total = if ($metric -eq 'line') { $linesValid }   else { $branchesValid }
    Write-Host ("  {0,-8} : {1,6:N2}%   floor {2,6:N2}%   target {3,6:N2}%   ({4}/{5})" -f `
        $metric, $actual[$metric], $policy.floor.$metric, $policy.target.$metric, $hit, $total)
}

# Worst classes first, so the next place to add tests is obvious.
$classes = @()
foreach ($cls in $xml.SelectNodes('//class')) {
    $lineNodes = $cls.SelectNodes('lines/line')
    $total = $lineNodes.Count
    if ($total -eq 0) { continue }
    $hit = @($lineNodes | Where-Object { [int]$_.hits -gt 0 }).Count
    $classes += [pscustomobject]@{
        Name = ($cls.name -split '\.')[-1]
        Pct  = Get-Pct $hit $total
        Hit  = $hit
        Total = $total
    }
}
$incomplete = @($classes | Where-Object { $_.Pct -lt 100 } | Sort-Object Pct, @{Expression='Total';Descending=$true})
Write-Host ("  classes  : {0} at 100% of {1}" -f ($classes.Count - $incomplete.Count), $classes.Count)
Write-Host ''

if ($incomplete.Count -gt 0) {
    Write-Host ("  {0} class(es) short of 100%. Largest gaps:" -f $incomplete.Count)
    foreach ($c in ($incomplete | Select-Object -First 12)) {
        Write-Host ("    {0,6:N2}%  {1,-34} ({2}/{3})" -f $c.Pct, $c.Name, $c.Hit, $c.Total)
    }
    Write-Host ''
}

# ------------------------------------------------------------------ verdict
$failures = @()
foreach ($metric in @('line', 'branch')) {
    if ($actual[$metric] -lt ([double]$policy.floor.$metric - 1e-9)) {
        $failures += ("{0} coverage {1:N2}% is below the floor of {2:N2}%" -f `
            $metric, $actual[$metric], $policy.floor.$metric)
    }
}

if ($failures.Count -gt 0) {
    foreach ($f in $failures) { Write-Host "FAIL: $f" -ForegroundColor Red }
    Write-Host ''
    Write-Host 'Coverage regressed. Add tests -- do not lower the floor.' -ForegroundColor Red
    exit 1
}

$slack = ($actual.Keys | ForEach-Object { $actual[$_] - [double]$policy.floor.$_ } | Measure-Object -Minimum).Minimum
if ($slack -gt [double]$policy.slackWarning) {
    Write-Host ("NOTE: every metric is at least {0:N2} points above its floor." -f $slack)
    Write-Host '      Raise "floor" in coverage-policy.json to lock the gain in.'
    Write-Host ''
}

Write-Host 'Coverage gate passed.' -ForegroundColor Green
