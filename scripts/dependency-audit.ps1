# scripts/dependency-audit.ps1
# Audits dependencies across all Mirror projects for security and privacy compliance

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Resolve-Path (Join-Path $scriptDir "..")

Write-Host "====================================================" -ForegroundColor Cyan
Write-Host "  RUNNING PRIVACY & SECURITY DEPENDENCY AUDIT" -ForegroundColor Cyan
Write-Host "====================================================" -ForegroundColor Cyan

$forbiddenPackages = @(
    "System.Net.Http",
    "System.Net.Sockets",
    "Microsoft.AspNetCore.SignalR",
    "Microsoft.ApplicationInsights",
    "Google.Analytics",
    "Segment.Analytics",
    "FirebaseAdmin",
    "Sentry",
    "Mixpanel"
)

$violations = @()
$checkedProjects = 0

$csprojs = Get-ChildItem -Path (Join-Path $rootDir "src") -Filter "*.csproj" -Recurse

foreach ($proj in $csprojs) {
    $checkedProjects++
    [xml]$xml = Get-Content $proj.FullName
    $nodes = $xml.SelectNodes("//PackageReference")
    foreach ($node in $nodes) {
        $name = $node.Include
        foreach ($forbidden in $forbiddenPackages) {
            if ($name -like "*$forbidden*") {
                $violations += "Project '$($proj.Name)' references forbidden telemetry/network package: '$name'"
            }
        }
    }
}

Write-Host "Audited $checkedProjects projects against $(@($forbiddenPackages).Count) forbidden privacy patterns."

if ($violations.Count -gt 0) {
    Write-Host "[FAIL] Privacy violations found:" -ForegroundColor Red
    foreach ($v in $violations) {
        Write-Host "  - $v" -ForegroundColor Red
    }
    exit 1
} else {
    Write-Host "[PASS] 100% Privacy Clean: Zero forbidden network or cloud telemetry packages found." -ForegroundColor Green
    exit 0
}
