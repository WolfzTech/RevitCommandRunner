param(
    [string]$Version = "1.0.0",
    [string]$OutputDir = "..\releases"
)

$ErrorActionPreference = "Stop"

$installerRoot = $PSScriptRoot
$repoRoot = Split-Path -Parent $installerRoot
$bundleDir = Join-Path $repoRoot "build\RevitCommandRunner.bundle"
$manifestPath = Join-Path $installerRoot "RevitCommandRunner.addin"
$wxsPath = Join-Path $installerRoot "wix\RevitCommandRunner.wxs"
$outputRoot = Join-Path $installerRoot $OutputDir
$msiPath = Join-Path $outputRoot "RevitCommandRunner-$Version.msi"

if (-not (Test-Path $bundleDir)) {
    throw "Bundle not found: $bundleDir. Run src\RevitCommandRunner\Build-AllVersions.ps1 first."
}

if (-not (Test-Path $manifestPath)) {
    throw "Manifest not found: $manifestPath"
}

if (-not (Test-Path $wxsPath)) {
    throw "WiX source not found: $wxsPath"
}

New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null

$localToolDir = Join-Path $repoRoot ".tools"
New-Item -ItemType Directory -Path $localToolDir -Force | Out-Null

$wixExe = Join-Path $localToolDir "wix.exe"
if (-not (Test-Path $wixExe)) {
    dotnet tool install wix --tool-path $localToolDir
}

& $wixExe build $wxsPath `
    -d "BundleDir=$bundleDir" `
    -d "ManifestPath=$manifestPath" `
    -o $msiPath

if ($LASTEXITCODE -ne 0) {
    throw "WiX build failed."
}

Write-Host "Created MSI: $msiPath" -ForegroundColor Green
