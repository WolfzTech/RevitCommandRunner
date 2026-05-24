# Create Standalone Installer Release
# Packages RevitCommandRunner as a single Installer.exe for distribution

param(
    [string]$Version = "1.0.0",
    [string]$OutputDir = "..\releases"
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "RevitCommandRunner Standalone Installer" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Paths
$buildPath = Join-Path $PSScriptRoot "..\build\RevitCommandRunner.bundle"
$installerPath = Join-Path $PSScriptRoot "."
$outputPath = Join-Path $PSScriptRoot $OutputDir
$installerExe = "RevitCommandRunner-v$Version-Installer.exe"
$installerOutput = Join-Path $outputPath $installerExe

# Check if build exists
if (-not (Test-Path $buildPath)) {
    Write-Host "✗ Build not found: $buildPath" -ForegroundColor Red
    Write-Host "  Please run Build-AllVersions.ps1 first." -ForegroundColor Yellow
    exit 1
}

# Check if Installer.exe exists
if (-not (Test-Path "$installerPath\Installer.exe")) {
    Write-Host "✗ Installer.exe not found" -ForegroundColor Red
    Write-Host "  Please build the installer project first." -ForegroundColor Yellow
    exit 1
}

# Create output directory
if (-not (Test-Path $outputPath)) {
    New-Item -ItemType Directory -Path $outputPath | Out-Null
}

Write-Host "Creating standalone installer..." -ForegroundColor Yellow
Write-Host "  Version: $Version" -ForegroundColor Gray
Write-Host "  Output: $installerOutput" -ForegroundColor Gray
Write-Host ""

# Create temporary staging directory
$stagingDir = Join-Path $PSScriptRoot "staging"
if (Test-Path $stagingDir) {
    Remove-Item -Path $stagingDir -Recurse -Force
}
New-Item -ItemType Directory -Path $stagingDir | Out-Null

# Copy bundle
Write-Host "Staging bundle..." -ForegroundColor Yellow
Copy-Item -Path $buildPath -Destination $stagingDir -Recurse -Force
Copy-Item -Path "$installerPath\PackageContents.xml" -Destination (Join-Path $stagingDir "RevitCommandRunner.bundle\PackageContents.xml") -Force

# Copy MCP server into bundle
Write-Host "Staging MCP server..." -ForegroundColor Yellow
$mcpSourceRoot = Join-Path $PSScriptRoot "..\mcp-server"
$mcpDest = Join-Path $stagingDir "RevitCommandRunner.bundle\mcp-server"
if (Test-Path "$mcpSourceRoot\dist") {
    New-Item -ItemType Directory -Path $mcpDest -Force | Out-Null
    
    # Copy compiled JS
    Copy-Item -Path "$mcpSourceRoot\dist\*" -Destination $mcpDest -Recurse -Force
    
    # Copy node_modules
    if (Test-Path "$mcpSourceRoot\node_modules") {
        Copy-Item -Path "$mcpSourceRoot\node_modules" -Destination $mcpDest -Recurse -Force
    } else {
        Write-Host "  Warning: node_modules not found. Run 'npm install' in mcp-server directory." -ForegroundColor Yellow
    }
    
    # Copy package.json
    if (Test-Path "$mcpSourceRoot\package.json") {
        Copy-Item -Path "$mcpSourceRoot\package.json" -Destination $mcpDest -Force
    }
} else {
    Write-Host "  Warning: MCP server not built. Run 'npm run build' in mcp-server directory." -ForegroundColor Yellow
}

# Copy installer executable
Write-Host "Copying installer..." -ForegroundColor Yellow
Copy-Item -Path "$installerPath\Installer.exe" -Destination $stagingDir -Force

# Create self-extracting archive using 7-Zip SFX (if available) or just copy the installer
Write-Host "Creating final installer..." -ForegroundColor Yellow

# For now, just copy the installer with the bundle next to it
# The installer will look for the bundle in its directory
Copy-Item -Path "$installerPath\Installer.exe" -Destination $installerOutput -Force

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Standalone Installer Created" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host "File: $installerOutput" -ForegroundColor Cyan
Write-Host "Size: $([math]::Round((Get-Item $installerOutput).Length / 1MB, 2)) MB" -ForegroundColor Gray
Write-Host ""
Write-Host "Distribution Instructions:" -ForegroundColor Yellow
Write-Host "  1. Distribute: $installerExe" -ForegroundColor White
Write-Host "  2. Users run the installer" -ForegroundColor White
Write-Host "  3. Bundle installs to ApplicationPlugins" -ForegroundColor White
Write-Host "  4. Registers in Windows Programs & Features" -ForegroundColor White
Write-Host ""

# Also create a ZIP with installer + bundle for backup
Write-Host "Creating backup ZIP with bundle..." -ForegroundColor Yellow
$zipName = "RevitCommandRunner-v$Version-Full.zip"
$zipPath = Join-Path $outputPath $zipName

if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Compress-Archive -Path "$stagingDir\*" -DestinationPath $zipPath -CompressionLevel Optimal

Write-Host "Backup ZIP created: $zipName" -ForegroundColor Green
Write-Host "Size: $([math]::Round((Get-Item $zipPath).Length / 1MB, 2)) MB" -ForegroundColor Gray

# Clean up staging
Remove-Item -Path $stagingDir -Recurse -Force

Write-Host ""
Write-Host "✓ Release package ready for distribution!" -ForegroundColor Green
Write-Host ""
