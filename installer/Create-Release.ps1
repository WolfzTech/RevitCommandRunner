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
# RevitCommandRunner v$Version

AI-driven command execution framework for Autodesk Revit with hot-reload support.

## Supported Revit Versions

- Revit 2021-2024 (.NET Framework 4.8)
- Revit 2025-2026 (.NET 8.0)
- Revit 2027 (.NET 10.0)

## Installation

### Quick Install

1. Run ``Installer.exe``
2. Start Revit
3. The add-in will load automatically

PowerShell alternative: run ``Install.ps1``.

### Manual Install

1. Copy the appropriate DLL from ``RevitCommandRunner.bundle\Contents\[YEAR]\`` to:
   ``%APPDATA%\Autodesk\Revit\Addins\[YEAR]\``

2. Copy ``RevitCommandRunner.addin`` to the same folder

3. Start Revit

## Configuration

The add-in creates a configuration file at:
``%LOCALAPPDATA%\RevitCommandRunner\config.json``

Default settings:
- Command queue polling: 500ms
- Console log capture: Enabled
- Queue file: ``%LOCALAPPDATA%\RevitCommandRunner\command-queue.json``

## Usage

### With MCP Server (OpenCode/Claude)

Configure the MCP server in your AI tool:

````json
{
  "mcpServers": {
    "revit-command-runner": {
      "command": "node",
      "args": ["D:/RevitCommandRunner/mcp-server/build/index.js"]
    }
  }
}
````

Then ask your AI assistant:
- "Execute HelloRevitCommand from the sample plugin"
- "Build and test my Revit plugin"
- "Create 5 walls in the active document"

### With PowerShell

````powershell
Import-Module D:\RevitCommandRunner\tools\RevitCommandRunner.psm1

`$result = Invoke-RevitCommand ``
    -DllPath "C:\MyPlugin\bin\Debug\MyPlugin.dll" ``
    -CommandClassName "MyNamespace.MyCommand"

Write-Host `$result.message
````

## Hot-Reload

RevitCommandRunner supports hot-reload for user plugins:

1. Run your command
2. Modify your code
3. Rebuild (Revit still running!)
4. Run again - changes applied immediately

No Revit restart needed!

## Sample Plugin

See ``samples\SamplePlugin\`` for example commands:
- **HelloRevitCommand**: Read document info
- **CreateWallsCommand**: Create walls with transactions

## Documentation

- **README.md**: Overview and features
- **USAGE_GUIDE.md**: Quick reference for AI usage
- **HOT_RELOAD_EXPLAINED.md**: Technical details on hot-reload

## Support

- GitHub: https://github.com/yourusername/RevitCommandRunner
- Issues: https://github.com/yourusername/RevitCommandRunner/issues

## License

MIT License - See LICENSE file for details

## Version History

### v$Version
- Multi-version support (Revit 2021-2027)
- Hot-reload via collectible AssemblyLoadContext on Revit 2025+ and Assembly.Load(bytes) fallback on older Revit
- MCP server integration
- Sample plugins included
"@

Set-Content -Path (Join-Path $releaseDir "README.txt") -Value $releaseReadme

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
