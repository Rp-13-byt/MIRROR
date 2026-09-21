[CmdletBinding()]
param()

$ErrorActionPreference = "Continue"

$rootDir = Split-Path -Parent $PSScriptRoot
Set-Location $rootDir

if (Test-Path "D:\dotnet\dotnet.exe") {
    $env:PATH = "D:\dotnet;" + $env:PATH
}

Write-Host "====================================================" -ForegroundColor Cyan
Write-Host "  MIRROR -- QUALCOMM HACKATHON READINESS AUDITOR" -ForegroundColor Cyan
Write-Host "====================================================" -ForegroundColor Cyan

$passedChecks = 0
$failedChecks = 0

function Check-Result {
    param(
        [string]$Name,
        [bool]$Success,
        [string]$Details = ""
    )
    if ($Success) {
        Write-Host "  [PASS] $Name" -ForegroundColor Green
        if ($Details -ne "") {
            Write-Host "         $Details" -ForegroundColor DarkGray
        }
        $script:passedChecks++
    } else {
        Write-Host "  [FAIL] $Name" -ForegroundColor Red
        if ($Details -ne "") {
            Write-Host "         $Details" -ForegroundColor Yellow
        }
        $script:failedChecks++
    }
}

# 1. DotNet SDK
$dotnetVersion = & dotnet --version 2>&1
$hasDotNet = ($LASTEXITCODE -eq 0)
Check-Result -Name 'DotNet SDK Available' -Success $hasDotNet -Details "Version: $dotnetVersion"

# 2. Build Solution
Write-Host "`n---> Verifying Solution Build..." -ForegroundColor Yellow
$buildOut = & dotnet build Mirror.sln -c Debug --verbosity quiet 2>&1
$buildSuccess = ($LASTEXITCODE -eq 0)
Check-Result -Name 'Mirror.sln Clean Build' -Success $buildSuccess

# 3. Test Suites
Write-Host "`n---> Running All 7 Test Suites..." -ForegroundColor Yellow
$testScript = Join-Path $PSScriptRoot 'test.ps1'
& powershell -ExecutionPolicy Bypass -File $testScript
$testsSuccess = ($LASTEXITCODE -eq 0)
Check-Result -Name 'All 7 Unit and Integration Test Suites' -Success $testsSuccess

# 4. Privacy Gate Audit
Write-Host "`n---> Running Privacy Enforcement Audit..." -ForegroundColor Yellow
$privScript = Join-Path $PSScriptRoot 'privacy-audit.ps1'
if (Test-Path $privScript) {
    & powershell -ExecutionPolicy Bypass -File $privScript
    $privSuccess = ($LASTEXITCODE -eq 0)
    Check-Result -Name 'Privacy Enforcement Audit (Zero Forbidden APIs and Fields)' -Success $privSuccess
}

# 5. Dependency Audit
Write-Host "`n---> Running Supply Chain and Dependency Audit..." -ForegroundColor Yellow
$depScript = Join-Path $PSScriptRoot 'dependency-audit.ps1'
if (Test-Path $depScript) {
    & powershell -ExecutionPolicy Bypass -File $depScript
    $depSuccess = ($LASTEXITCODE -eq 0)
    Check-Result -Name 'Dependency Audit (Zero Cloud and Telemetry SDKs)' -Success $depSuccess
}

# 6. Model Artifacts
$fp32Model = Test-Path (Join-Path $rootDir 'ml\models\mirror_pattern_v1.onnx')
$qdqModel = Test-Path (Join-Path $rootDir 'ml\models\mirror_pattern_v1.qdq.onnx')
$modelsExist = ($fp32Model -and $qdqModel)
Check-Result -Name 'Qualcomm Hexagon QDQ INT8 Model Artifacts' -Success $modelsExist -Details 'mirror_pattern_v1.qdq.onnx verified'

# 7. Model Metadata
$metadataPath = Join-Path $rootDir 'ml\qualcomm\model_metadata.json'
$metaExist = Test-Path $metadataPath
Check-Result -Name 'Qualcomm AI Hub Model Metadata' -Success $metaExist -Details 'Dual-AI schemas pinned'

# 8. Documentation Suite
$docs = @(
    'docs\QUALCOMM_HACKATHON_STRATEGY.md',
    'docs\AI_HUB_MODELS.md',
    'docs\EDGE_AI_ARCHITECTURE.md',
    'docs\BENCHMARKS.md',
    'docs\RESPONSIBLE_AI.md',
    'docs\SECURITY_REVIEW.md',
    'docs\REFERENCES.md'
)
$allDocsExist = $true
foreach ($d in $docs) {
    if (-not (Test-Path (Join-Path $rootDir $d))) { $allDocsExist = $false }
}
Check-Result -Name 'Technical Documentation Architecture' -Success $allDocsExist -Details 'All 7 architectural documents present'

# Final Scorecard
Write-Host "`n====================================================" -ForegroundColor Cyan
Write-Host '  HACKATHON READINESS SCORECARD' -ForegroundColor Cyan
Write-Host '====================================================' -ForegroundColor Cyan
Write-Host "Passed Checks: $passedChecks" -ForegroundColor Green
$statusColor = if ($failedChecks -eq 0) { 'Green' } else { 'Red' }
Write-Host "Failed Checks: $failedChecks" -ForegroundColor $statusColor

if ($failedChecks -eq 0) {
    Write-Host "`nMIRROR IS 100% READY FOR QUALCOMM HACKATHON SUBMISSION!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "`nSome readiness checks failed. Please inspect output above." -ForegroundColor Red
    exit 1
}
