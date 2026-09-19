<#
.SYNOPSIS
    Builds the complete Mirror solution.
.DESCRIPTION
    Compiles all core, tracking, persistence, analytics, inference, platform, and WinUI 3 desktop projects.
#>
[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [switch]$Clean
)

$ErrorActionPreference = "Stop"

if (Test-Path "D:\dotnet\dotnet.exe") {
    $env:PATH = "D:\dotnet;" + $env:PATH
}

Write-Host "====================================================" -ForegroundColor Cyan
Write-Host "  BUILDING MIRROR SOLUTION ($Configuration)" -ForegroundColor Cyan
Write-Host "====================================================" -ForegroundColor Cyan

$solutionPath = "D:\MIRROR\Mirror.sln"

if ($Clean) {
    Write-Host "[INFO] Cleaning previous build artifacts..." -ForegroundColor Yellow
    dotnet clean $solutionPath -c $Configuration
}

Write-Host "[INFO] Running dotnet build..." -ForegroundColor Cyan
dotnet build $solutionPath -c $Configuration --no-incremental

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n[SUCCESS] Mirror built successfully with 0 errors!" -ForegroundColor Green
} else {
    Write-Error "[FAIL] Build failed with exit code $LASTEXITCODE."
}
