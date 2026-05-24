# Create Release Bundle
# Packages RevitCommandRunner for distribution

param(
    [string]$Version = "1.0.0",
    [string]$OutputDir = "..\releases"
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "RevitCommandRunner Release Packager" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Paths
$buildPath = Join-Path $PSScriptRoot "..\build\RevitCommandRunner.bundle"
$installerPath = Join-Path $PSScriptRoot "."
$outputPath = Join-Path $PSScriptRoot $OutputDir
$releaseName = "RevitCommandRunner-v$Version"
$releaseDir = Join-Path $outputPath $releaseName

# Check if build exists
if (-not (Test-Path $buildPath)) {
    Write-Host "✗ Build not found: $buildPath" -ForegroundColor Red
    Write-Host "  Please run Build-AllVersions.ps1 first." -ForegroundColor Yellow
    exit 1
}

# Create release directory
if (Test-Path $releaseDir) {
    Remove-Item -Path $releaseDir -Recurse -Force
}
New-Item -ItemType Directory -Path $releaseDir | Out-Null

Write-Host "Creating release package..." -ForegroundColor Yellow
Write-Host "  Version: $Version" -ForegroundColor Gray
Write-Host "  Output: $releaseDir" -ForegroundColor Gray
Write-Host ""

# Copy bundle
Write-Host "Copying bundle..." -ForegroundColor Yellow
Copy-Item -Path $buildPath -Destination $releaseDir -Recurse -Force
Copy-Item -Path "$installerPath\PackageContents.xml" -Destination (Join-Path $releaseDir "RevitCommandRunner.bundle\PackageContents.xml") -Force

# Copy MCP server into bundle
Write-Host "Copying MCP server..." -ForegroundColor Yellow
$mcpSourceRoot = Join-Path $PSScriptRoot "..\mcp-server"
$mcpDest = Join-Path $releaseDir "RevitCommandRunner.bundle\mcp-server"
if (Test-Path "$mcpSourceRoot\dist") {
    New-Item -ItemType Directory -Path $mcpDest -Force | Out-Null
    
    # Copy compiled JS
    Copy-Item -Path "$mcpSourceRoot\dist\*" -Destination $mcpDest -Recurse -Force
    
    # Copy node_modules (only production dependencies)
    if (Test-Path "$mcpSourceRoot\node_modules") {
        Copy-Item -Path "$mcpSourceRoot\node_modules" -Destination $mcpDest -Recurse -Force
    } else {
        Write-Host "  Warning: node_modules not found. Run 'npm install' in mcp-server directory." -ForegroundColor Yellow
    }
    
    # Copy package.json (needed for module resolution)
    if (Test-Path "$mcpSourceRoot\package.json") {
        Copy-Item -Path "$mcpSourceRoot\package.json" -Destination $mcpDest -Force
    }
} else {
    Write-Host "  Warning: MCP server not built. Run 'npm run build' in mcp-server directory." -ForegroundColor Yellow
}

# Copy installer files
Write-Host "Copying installer..." -ForegroundColor Yellow
Copy-Item -Path "$installerPath\Install.ps1" -Destination $releaseDir
Copy-Item -Path "$installerPath\RevitCommandRunner.addin" -Destination $releaseDir
if (Test-Path "$installerPath\Installer.exe") {
    Copy-Item -Path "$installerPath\Installer.exe" -Destination $releaseDir
}

# Copy documentation
Write-Host "Copying documentation..." -ForegroundColor Yellow
$docsSource = Join-Path $PSScriptRoot ".."
$docFiles = @("README.md", "INSTALLATION.md", "USAGE_GUIDE.md", "HOT_RELOAD_EXPLAINED.md", "ASSEMBLY_UNLOADING.md", "LICENSE")
foreach ($doc in $docFiles) {
    $sourcePath = Join-Path $docsSource $doc
    if (Test-Path $sourcePath) {
        Copy-Item -Path $sourcePath -Destination $releaseDir -ErrorAction SilentlyContinue
    }
}

# Copy sample plugin
Write-Host "Copying sample plugin..." -ForegroundColor Yellow
$sampleSource = Join-Path $docsSource "samples\SamplePlugin"
$sampleDest = Join-Path $releaseDir "samples\SamplePlugin"
if (Test-Path $sampleSource) {
    New-Item -ItemType Directory -Path $sampleDest -Force | Out-Null
    Copy-Item -Path "$sampleSource\*.cs" -Destination $sampleDest -ErrorAction SilentlyContinue
    Copy-Item -Path "$sampleSource\*.csproj" -Destination $sampleDest -ErrorAction SilentlyContinue
    Copy-Item -Path "$sampleSource\README.md" -Destination $sampleDest -ErrorAction SilentlyContinue
}

# Create README for release
Write-Host "Creating release README..." -ForegroundColor Yellow
$releaseReadme = @"
# RevitCommandRunner v$Version - Installer Package

AI-driven command execution framework for Autodesk Revit with hot-reload support.

## Installation

**Run Installer.exe** - That's it!

The installer will:
- Install the bundle to ApplicationPlugins
- Register in Windows Programs & Features
- Include MCP server with dependencies

## Supported Revit Versions

- Revit 2021-2024 (.NET Framework 4.8)
- Revit 2025-2026 (.NET 8.0)
- Revit 2027 (.NET 10.0)

## After Installation

### Configure AI Agent (Optional)

**For OpenCode:**
Edit ``%USERPROFILE%\.config\opencode\opencode.jsonc``:
````json
{
  "mcp": {
    "revit-command-runner": {
      "type": "local",
      "command": [
        "node",
        "%APPDATA%/Autodesk/ApplicationPlugins/RevitCommandRunner.bundle/mcp-server/index.js"
      ],
      "enabled": true
    }
  }
}
````

**For Claude Desktop:**
Edit ``%APPDATA%\Claude\claude_desktop_config.json``:
````json
{
  "mcpServers": {
    "revit-command-runner": {
      "command": "node",
      "args": ["%APPDATA%/Autodesk/ApplicationPlugins/RevitCommandRunner.bundle/mcp-server/index.js"]
    }
  }
}
````

### Start Revit

The add-in loads automatically. No dialogs, no UI.

## Uninstallation

**Option 1:** Windows Settings → Apps → RevitCommandRunner → Uninstall

**Option 2:** Run ``Installer.exe --uninstall``

## Documentation

See the included documentation files for detailed information:
- README.md - Full documentation
- INSTALLATION.md - Installation guide
- USAGE_GUIDE.md - Usage examples
- HOT_RELOAD_EXPLAINED.md - How hot-reload works
- ASSEMBLY_UNLOADING.md - Technical details

## Sample Plugin

The ``samples/SamplePlugin`` directory contains example commands you can use as a starting point.

## Support

- GitHub: https://github.com/yourusername/RevitCommandRunner
- Issues: https://github.com/yourusername/RevitCommandRunner/issues

---

**Quick Start:** Run Installer.exe, configure your AI agent, start Revit!
"@

Set-Content -Path (Join-Path $releaseDir "README.txt") -Value $releaseReadme -Encoding UTF8

# Create ZIP archive
Write-Host "Creating ZIP archive..." -ForegroundColor Yellow
$zipPath = Join-Path $outputPath "$releaseName.zip"
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Compress-Archive -Path "$releaseDir\*" -DestinationPath $zipPath -CompressionLevel Optimal

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Release Package Created" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Version: $Version" -ForegroundColor Green
Write-Host "Folder: $releaseDir" -ForegroundColor Cyan
Write-Host "ZIP: $zipPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "✓ Release package ready for distribution!" -ForegroundColor Green
Write-Host ""

# Show contents
Write-Host "Package contents:" -ForegroundColor Yellow
Get-ChildItem -Path $releaseDir -Recurse -File | ForEach-Object {
    $relativePath = $_.FullName.Replace($releaseDir, "").TrimStart("\")
    Write-Host "  $relativePath" -ForegroundColor Gray
}

Write-Host ""
