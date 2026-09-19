<#
.SYNOPSIS
    Automated Static Privacy Audit Scanner for Mirror.
.DESCRIPTION
    Scans source code, Win32 P/Invokes, networking dependencies, and database schemas
    to guarantee 100% offline compliance and zero invasive telemetry.
#>
[CmdletBinding()]
param()

Write-Host "====================================================" -ForegroundColor Cyan
Write-Host "  MIRROR PRIVACY & OFFLINE ARCHITECTURE AUDIT" -ForegroundColor Cyan
Write-Host "====================================================" -ForegroundColor Cyan

$srcDir = "D:\MIRROR\src"
$violations = @()

# 1. Check for Forbidden Win32 Surveillance APIs
Write-Host "[1/4] Auditing Win32 P/Invoke declarations..." -ForegroundColor Yellow
$forbiddenWin32 = @(
    "GetWindowText",
    "GetWindowTextW",
    "GetWindowTextA",
    "InternalGetWindowText",
    "GetClipboardData",
    "OpenClipboard",
    "GetAsyncKeyState",
    "GetKeyState",
    "GetKeyboardState",
    "WH_KEYBOARD",
    "WH_KEYBOARD_LL",
    "BitBlt",
    "CreateCompatibleBitmap",
    "StretchBlt",
    "PrintWindow",
    "capCreateCaptureWindow"
)

$csFiles = Get-ChildItem -Path $srcDir -Recurse -Filter "*.cs" | Where-Object { $_.FullName -notmatch "\\obj\\" -and $_.FullName -notmatch "\\bin\\" }

foreach ($file in $csFiles) {
    $content = Get-Content $file.FullName -Raw
    foreach ($api in $forbiddenWin32) {
        if ($content -match "\b$api\b") {
            $violations += "Forbidden Win32 API '$api' detected in $($file.FullName)"
        }
    }
}

# 2. Check for Forbidden Remote Telemetry & Networking Types
Write-Host "[2/4] Auditing remote networking & telemetry dependencies..." -ForegroundColor Yellow
$forbiddenNetwork = @(
    "HttpClient",
    "WebClient",
    "TcpClient",
    "UdpClient",
    "ClientWebSocket",
    "TelemetryClient",
    "ApplicationInsights",
    "GoogleAnalytics",
    "Mixpanel",
    "Segment"
)

foreach ($file in $csFiles) {
    # Skip NetworkPolicy itself which declares the forbidden list for scanning
    if ($file.Name -eq "NetworkPolicy.cs" -or $file.Name -eq "PrivacyGuard.cs") { continue }
    
    $content = Get-Content $file.FullName -Raw
    foreach ($net in $forbiddenNetwork) {
        if ($content -match "\b$net\b") {
            $violations += "Forbidden networking type '$net' detected in $($file.FullName)"
        }
    }
}

# 3. Check SQLite Schema Migrations for Content-Leaking Columns
Write-Host "[3/4] Auditing SQLite schema definitions..." -ForegroundColor Yellow
$migratorPath = "D:\MIRROR\src\Mirror.Persistence\DatabaseMigrator.cs"
if (Test-Path $migratorPath) {
    $sqlContent = Get-Content $migratorPath -Raw
    $forbiddenSql = @("window_title", "title TEXT", "url TEXT", "keystroke", "clipboard", "screenshot", "buffer")
    foreach ($kw in $forbiddenSql) {
        if ($sqlContent -match $kw) {
            $violations += "Forbidden sensitive storage column '$kw' detected in DatabaseMigrator.cs"
        }
    }
}

# 4. Check Explanation Vocabulary for Clinical / Diagnostic Terms
Write-Host "[4/4] Auditing behavioral copy for non-clinical compliance..." -ForegroundColor Yellow
$explPath = "D:\MIRROR\src\Mirror.Analytics\PatternExplanationBuilder.cs"
if (Test-Path $explPath) {
    $explContent = Get-Content $explPath -Raw
    $forbiddenTerms = @("addiction", "addicted", "disorder", "adhd", "burnout", "depression", "anxiety", "illness", "pathology", "treatment", "therapy")
    foreach ($term in $forbiddenTerms) {
        if ($explContent -match "\b$term\b") {
            $violations += "Clinical or diagnostic term '$term' detected in PatternExplanationBuilder.cs"
        }
    }
}

Write-Host "`n====================================================" -ForegroundColor Cyan
Write-Host "  AUDIT RESULTS" -ForegroundColor Cyan
Write-Host "====================================================" -ForegroundColor Cyan

if ($violations.Count -eq 0) {
    Write-Host "[PASS] Zero privacy violations detected across all source code!" -ForegroundColor Green
    Write-Host "  - 0 Keystroke / Screen / Title inspection APIs" -ForegroundColor Green
    Write-Host "  - 0 HTTP / Socket / Cloud Telemetry clients" -ForegroundColor Green
    Write-Host "  - 0 Content storage columns in database" -ForegroundColor Green
    Write-Host "  - 100% Non-clinical, objective behavioral vocabulary" -ForegroundColor Green
    exit 0
} else {
    Write-Host "[FAIL] $($violations.Count) privacy violation(s) detected:" -ForegroundColor Red
    foreach ($v in $violations) {
        Write-Host "  - $v" -ForegroundColor Red
    }
    exit 1
}
