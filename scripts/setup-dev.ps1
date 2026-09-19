<#
.SYNOPSIS
    Environment verification and setup script for Mirror.
.DESCRIPTION
    Validates .NET 8 SDK, Python environment, NuGet configuration, and system resources.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Write-Host "====================================================" -ForegroundColor Cyan
Write-Host "  MIRROR DEVELOPMENT ENVIRONMENT SETUP & VERIFICATION" -ForegroundColor Cyan
Write-Host "====================================================" -ForegroundColor Cyan

# 1. PATH & .NET 8 Check
if (Test-Path "D:\dotnet\dotnet.exe") {
    $env:PATH = "D:\dotnet;" + $env:PATH
    Write-Host "[OK] Portable .NET SDK path prepended: D:\dotnet" -ForegroundColor Green
}

try {
    $dotnetVer = dotnet --version
    Write-Host "[OK] .NET SDK Version: $dotnetVer" -ForegroundColor Green
} catch {
    Write-Error ".NET SDK not found! Please install .NET 8 SDK or place in D:\dotnet"
}

# 2. Disk Space Verification
$drives = Get-PSDrive -PSProvider FileSystem
foreach ($d in $drives) {
    $freeGb = [math]::Round($d.Free / 1GB, 2)
    $usedGb = [math]::Round($d.Used / 1GB, 2)
    $totalGb = $freeGb + $usedGb
    if ($freeGb -lt 1.0) {
        Write-Host "[WARN] Drive $($d.Name): $freeGb GB free of $totalGb GB (CRITICALLY LOW)" -ForegroundColor Yellow
    } else {
        Write-Host "[OK] Drive $($d.Name): $freeGb GB free of $totalGb GB" -ForegroundColor Green
    }
}

# 3. NuGet Global Cache Check
$nugetCache = "D:\nuget_packages"
if (Test-Path $nugetCache) {
    Write-Host "[OK] NuGet package cache located at $nugetCache" -ForegroundColor Green
} else {
    Write-Host "[INFO] Creating NuGet package cache directory at $nugetCache" -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $nugetCache -Force | Out-Null
}

# 4. Python & ML Tools Verification
try {
    $pyVer = python --version 2>&1
    Write-Host "[OK] Python: $pyVer" -ForegroundColor Green

    $torchCheck = python -c "import torch; import onnx; import onnxruntime; print(f'PyTorch {torch.__version__}, ONNX {onnx.__version__}, ORT {onnxruntime.__version__}')" 2>&1
    Write-Host "[OK] ML Stack: $torchCheck" -ForegroundColor Green
} catch {
    Write-Host "[WARN] Python or ML libraries not fully detected in current shell. (Required only for training/export)" -ForegroundColor Yellow
}

# 5. Restore Solution Dependencies
Write-Host "`nRestoring solution NuGet packages..." -ForegroundColor Cyan
dotnet restore D:\MIRROR\Mirror.sln --configfile D:\MIRROR\nuget.config

Write-Host "`nEnvironment is ready for building and testing Mirror!" -ForegroundColor Green
