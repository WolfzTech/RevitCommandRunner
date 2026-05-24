# Master Build and Release Script
# Builds all versions and creates release package

param(
    [string]$Version = "1.0.0",
    [string]$Configuration = "Release",
    [switch]$SkipBuild,
    [switch]$SkipPackage
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "RevitCommandRunner - Master Build" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Version: $Version" -ForegroundColor Green
Write-Host "Configuration: $Configuration" -ForegroundColor Green
Write-Host ""

$rootPath = Split-Path -Parent $PSScriptRoot
$srcPath = Join-Path $rootPath "src\RevitCommandRunner"
$installerPath = Join-Path $rootPath "installer"

# Step 1: Build all versions
if (-not $SkipBuild) {
    Write-Host "Step 1: Building all Revit versions..." -ForegroundColor Yellow
    Write-Host ""
    
    Push-Location $srcPath
    try {
        & .\Build-AllVersions.ps1 -Configuration $Configuration
        if ($LASTEXITCODE -ne 0) {
            throw "Build failed"
        }
    }
    finally {
        Pop-Location
    }
    
    Write-Host ""
    Write-Host "✓ Build completed" -ForegroundColor Green
    Write-Host ""
}
else {
    Write-Host "Step 1: Skipping build (using existing)" -ForegroundColor Yellow
    Write-Host ""
}

# Step 2: Create release package
if (-not $SkipPackage) {
    Write-Host "Step 2: Creating release package..." -ForegroundColor Yellow
    Write-Host ""
    
    Push-Location $installerPath
    try {
        & .\Create-Release.ps1 -Version $Version
        if ($LASTEXITCODE -ne 0) {
            throw "Packaging failed"
        }
    }
    finally {
        Pop-Location
    }
    
    Write-Host ""
    Write-Host "✓ Release package created" -ForegroundColor Green
    Write-Host ""
}
else {
    Write-Host "Step 2: Skipping packaging" -ForegroundColor Yellow
    Write-Host ""
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Build Complete!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Test installation: cd installer && .\Install.ps1" -ForegroundColor White
Write-Host "2. Start Revit and verify add-in loads" -ForegroundColor White
Write-Host "3. Test with sample plugin" -ForegroundColor White
Write-Host ""
Write-Host "Release package: releases\RevitCommandRunner-v$Version.zip" -ForegroundColor Cyan
Write-Host ""
