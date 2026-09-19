<#
.SYNOPSIS
    Launches Mirror on Windows 11.
#>
[CmdletBinding()]
param()

$appExe = "D:\MIRROR\src\Mirror.App\bin\Debug\net8.0-windows10.0.19041.0\win-x64\Mirror.App.exe"
if (-not (Test-Path $appExe)) {
    $appExe = "D:\MIRROR\src\Mirror.App\bin\x64\Debug\net8.0-windows10.0.19041.0\win-x64\Mirror.App.exe"
}

if (Test-Path "D:\dotnet") {
    $env:DOTNET_ROOT = "D:\dotnet"
    $env:PATH = "D:\dotnet;" + $env:PATH
}

if (Test-Path $appExe) {
    Write-Host "Launching Mirror: $appExe" -ForegroundColor Cyan
    Start-Process -FilePath $appExe -WorkingDirectory (Split-Path $appExe)
    Write-Host "Mirror is now running!" -ForegroundColor Green
} else {
    Write-Error "Mirror.App.exe not found. Please build first using scripts/build.ps1"
}
