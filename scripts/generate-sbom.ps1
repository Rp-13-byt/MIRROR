# scripts/generate-sbom.ps1
# Generates SPDX 2.3 SBOM for Mirror Windows Application

[CmdletBinding()]
param(
    [string]$OutputDir = "artifacts",
    [string]$OutputFile = "sbom.spdx.json"
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Resolve-Path (Join-Path $scriptDir "..")
$artifactsPath = Join-Path $rootDir $OutputDir

if (!(Test-Path $artifactsPath)) {
    New-Item -ItemType Directory -Path $artifactsPath -Force | Out-Null
}

$destinationPath = Join-Path $artifactsPath $OutputFile

Write-Host "====================================================" -ForegroundColor Cyan
Write-Host "  GENERATING SOFTWARE BILL OF MATERIALS (SBOM)" -ForegroundColor Cyan
Write-Host "====================================================" -ForegroundColor Cyan

$csprojs = Get-ChildItem -Path (Join-Path $rootDir "src") -Filter "*.csproj" -Recurse

$packages = @{}

foreach ($proj in $csprojs) {
    [xml]$xml = Get-Content $proj.FullName
    $nodes = $xml.SelectNodes("//PackageReference")
    foreach ($node in $nodes) {
        $name = $node.Include
        $version = $node.Version
        if ($name -and $version) {
            $key = "$name@$version"
            if (-not $packages.ContainsKey($key)) {
                $packages[$key] = @{
                    Name = $name
                    Version = $version
                    Projects = @($proj.Name)
                }
            } else {
                $packages[$key].Projects += $proj.Name
            }
        }
    }
}

$spdxPackages = @()
$pkgIndex = 1

foreach ($key in $packages.Keys | Sort-Object) {
    $pkg = $packages[$key]
    $spdxPackages += @{
        SPDXID = "SPDXRef-Package-$pkgIndex"
        name = $pkg.Name
        versionInfo = $pkg.Version
        downloadLocation = "https://www.nuget.org/packages/$($pkg.Name)/$($pkg.Version)"
        filesAnalyzed = $false
        licenseConcluded = "NOASSERTION"
        licenseDeclared = "NOASSERTION"
        copyrightText = "NOASSERTION"
        supplier = "Organization: NuGet.org"
        comment = "Utilized by: $($pkg.Projects -join ', ')"
    }
    $pkgIndex++
}

$sbom = @{
    spdxVersion = "SPDX-2.3"
    dataLicense = "CC0-1.0"
    SPDXID = "SPDXRef-DOCUMENT"
    name = "Mirror-Windows-App-SBOM"
    documentNamespace = "https://github.com/Rp-13-byt/MIRROR/spdx/$(New-Guid)"
    creationInfo = @{
        creators = @("Tool: Mirror-SBOM-Generator-1.0.0", "Organization: Mirror Development Team")
        created = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
    }
    packages = $spdxPackages
}

$json = $sbom | ConvertTo-Json -Depth 6
Set-Content -Path $destinationPath -Value $json -Encoding UTF8

Write-Host "Successfully generated SBOM containing $($spdxPackages.Count) packages." -ForegroundColor Green
Write-Host "Output written to: $destinationPath" -ForegroundColor Green
