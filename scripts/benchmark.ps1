<#
.SYNOPSIS
    Runs inference latency benchmarks for Mirror's ONNX models.
.DESCRIPTION
    Evaluates FP32 and INT8 QDQ ONNX models across warmup and steady-state inference loops.
#>
[CmdletBinding()]
param(
    [int]$Iterations = 100
)

$ErrorActionPreference = "Stop"

Write-Host "====================================================" -ForegroundColor Cyan
Write-Host "  MIRROR ON-DEVICE ML INFERENCE BENCHMARK" -ForegroundColor Cyan
Write-Host "====================================================" -ForegroundColor Cyan

$rootDir = Split-Path -Parent $PSScriptRoot
Set-Location $rootDir

$benchScript = Join-Path $rootDir "ml\benchmark.py"
if (Test-Path $benchScript) {
    python $benchScript --iterations $Iterations
} else {
    Write-Error "Benchmark script $benchScript not found."
}
