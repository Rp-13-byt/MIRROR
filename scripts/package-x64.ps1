<#
.SYNOPSIS
    Packages Mirror for Windows x64.
.DESCRIPTION
    Builds and publishes a x64 distribution of Mirror with bundled ONNX models.
#>
[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [string]$OutputDir = "D:\MIRROR\dist\win-x64"
)

$ErrorActionPreference = "Stop"

if (Test-Path "D:\dotnet\dotnet.exe") {
    $env:PATH = "D:\dotnet;" + $env:PATH
}

Write-Host "====================================================" -ForegroundColor Cyan
Write-Host "  PACKAGING MIRROR FOR WINDOWS X64" -ForegroundColor Cyan
Write-Host "====================================================" -ForegroundColor Cyan

$appProj = "D:\MIRROR\src\Mirror.App\Mirror.App.csproj"

if (Test-Path $OutputDir) {
    Remove-Item $OutputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

Write-Host "[INFO] Publishing win-x64 binaries..." -ForegroundColor Cyan
dotnet publish $appProj -c $Configuration -r win-x64 --self-contained false -o $OutputDir

# Copy models
$modelsDist = Join-Path $OutputDir "Models"
New-Item -ItemType Directory -Path $modelsDist -Force | Out-Null
Copy-Item "D:\MIRROR\src\Mirror.Inference\Models\*" $modelsDist -Recurse -Force

Write-Host "[SUCCESS] x64 package created at $OutputDir" -ForegroundColor Green
