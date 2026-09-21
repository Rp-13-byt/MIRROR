<#
.SYNOPSIS
    Runs all Mirror unit, integration, and privacy test suites.
#>
[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Continue"

$rootDir = Split-Path -Parent $PSScriptRoot
Set-Location $rootDir

if (Test-Path "D:\dotnet\dotnet.exe") {
    $env:PATH = "D:\dotnet;" + $env:PATH
}

Write-Host "====================================================" -ForegroundColor Cyan
Write-Host "  RUNNING ALL MIRROR TEST SUITES" -ForegroundColor Cyan
Write-Host "====================================================" -ForegroundColor Cyan

$testProjects = @(
    (Join-Path $rootDir "tests\Mirror.Core.Tests\Mirror.Core.Tests.csproj"),
    (Join-Path $rootDir "tests\Mirror.Tracking.Tests\Mirror.Tracking.Tests.csproj"),
    (Join-Path $rootDir "tests\Mirror.Persistence.Tests\Mirror.Persistence.Tests.csproj"),
    (Join-Path $rootDir "tests\Mirror.Analytics.Tests\Mirror.Analytics.Tests.csproj"),
    (Join-Path $rootDir "tests\Mirror.Inference.Tests\Mirror.Inference.Tests.csproj"),
    (Join-Path $rootDir "tests\Mirror.Privacy.Tests\Mirror.Privacy.Tests.csproj"),
    (Join-Path $rootDir "tests\Mirror.AI.Tests\Mirror.AI.Tests.csproj")
)

$totalPassed = 0
$totalFailed = 0
$failedSuites = @()

foreach ($proj in $testProjects) {
    $name = [System.IO.Path]::GetFileNameWithoutExtension($proj)
    Write-Host "`n---> Running test suite: $name" -ForegroundColor Yellow
    
    $output = dotnet test $proj -c $Configuration --logger "console;verbosity=minimal" 2>&1
    $output | ForEach-Object { Write-Host $_ }
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[PASS] $name" -ForegroundColor Green
        $totalPassed++
    } else {
        Write-Host "[FAIL] $name" -ForegroundColor Red
        $totalFailed++
        $failedSuites += $name
    }
}

Write-Host "`n====================================================" -ForegroundColor Cyan
Write-Host "  TEST RESULTS SUMMARY" -ForegroundColor Cyan
Write-Host "====================================================" -ForegroundColor Cyan
Write-Host "Total Suites Passed: $totalPassed" -ForegroundColor Green
$failColor = if ($totalFailed -gt 0) { "Red" } else { "Green" }
Write-Host "Total Suites Failed: $totalFailed" -ForegroundColor $failColor

if ($totalFailed -gt 0) {
    Write-Host "Failed suites: $($failedSuites -join ', ')" -ForegroundColor Red
    exit 1
} else {
    Write-Host "All test suites passed cleanly!" -ForegroundColor Green
    exit 0
}
