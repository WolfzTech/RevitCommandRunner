# RevitCommandRunner Installer
# Installs the add-in for all detected Revit versions (2021-2027)

param(
    [switch]$Uninstall,
    [string]$BuildPath = "..\build\RevitCommandRunner.bundle"
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "RevitCommandRunner Installer v1.0" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Define Revit versions
$revitVersions = @("2021", "2022", "2023", "2024", "2025", "2026", "2027")

# Get AppData path
$appData = [Environment]::GetFolderPath("ApplicationData")
$addinsBasePath = Join-Path $appData "Autodesk\Revit\Addins"

# Resolve build path
$buildPath = Join-Path $PSScriptRoot $BuildPath
if (-not (Test-Path $buildPath)) {
    Write-Host "✗ Build path not found: $buildPath" -ForegroundColor Red
    Write-Host "  Please run Build-AllVersions.ps1 first." -ForegroundColor Yellow
    exit 1
}

$installedCount = 0
$skippedCount = 0
$errorCount = 0

if ($Uninstall) {
    Write-Host "Uninstalling RevitCommandRunner..." -ForegroundColor Yellow
    Write-Host ""
    
    foreach ($year in $revitVersions) {
        $addinPath = Join-Path $addinsBasePath $year
        
        if (-not (Test-Path $addinPath)) {
            Write-Host "  Revit $year - Not installed" -ForegroundColor Gray
            $skippedCount++
            continue
        }
        
        $addinFile = Join-Path $addinPath "RevitCommandRunner.addin"
        $dllPath = Join-Path $addinPath "RevitCommandRunner.dll"
        
        try {
            if (Test-Path $addinFile) {
                Remove-Item $addinFile -Force
            }
            if (Test-Path $dllPath) {
                Remove-Item $dllPath -Force
            }
            
            # Remove additional files
            $filesToRemove = @("RevitCommandRunner.pdb", "Newtonsoft.Json.dll")
            foreach ($file in $filesToRemove) {
                $filePath = Join-Path $addinPath $file
                if (Test-Path $filePath) {
                    Remove-Item $filePath -Force -ErrorAction SilentlyContinue
                }
            }
            
            Write-Host "  Revit $year - ✓ Uninstalled" -ForegroundColor Green
            $installedCount++
        }
        catch {
            Write-Host "  Revit $year - ✗ Error: $_" -ForegroundColor Red
            $errorCount++
        }
    }
}
else {
    Write-Host "Installing RevitCommandRunner..." -ForegroundColor Yellow
    Write-Host ""
    
    foreach ($year in $revitVersions) {
        $addinPath = Join-Path $addinsBasePath $year
        
        # Check if Revit version is installed
        if (-not (Test-Path $addinPath)) {
            Write-Host "  Revit $year - Not installed (skipped)" -ForegroundColor Gray
            $skippedCount++
            continue
        }
        
        # Check if build exists for this version
        $sourcePath = Join-Path $buildPath "Contents\$year"
        if (-not (Test-Path $sourcePath)) {
            Write-Host "  Revit $year - Build not found (skipped)" -ForegroundColor Yellow
            $skippedCount++
            continue
        }
        
        try {
            # Copy .addin file
            $addinFile = Join-Path $PSScriptRoot "RevitCommandRunner.addin"
            $destAddinFile = Join-Path $addinPath "RevitCommandRunner.addin"
            Copy-Item -Path $addinFile -Destination $destAddinFile -Force
            
            # Copy DLL and dependencies
            $dllFile = Join-Path $sourcePath "RevitCommandRunner.dll"
            $destDllFile = Join-Path $addinPath "RevitCommandRunner.dll"
            Copy-Item -Path $dllFile -Destination $destDllFile -Force
            
            # Copy PDB if exists
            $pdbFile = Join-Path $sourcePath "RevitCommandRunner.pdb"
            if (Test-Path $pdbFile) {
                $destPdbFile = Join-Path $addinPath "RevitCommandRunner.pdb"
                Copy-Item -Path $pdbFile -Destination $destPdbFile -Force
            }
            
            # Copy Newtonsoft.Json.dll if exists
            $jsonDll = Join-Path $sourcePath "Newtonsoft.Json.dll"
            if (Test-Path $jsonDll) {
                $destJsonDll = Join-Path $addinPath "Newtonsoft.Json.dll"
                Copy-Item -Path $jsonDll -Destination $destJsonDll -Force
            }
            
            Write-Host "  Revit $year - ✓ Installed" -ForegroundColor Green
            $installedCount++
        }
        catch {
            Write-Host "  Revit $year - ✗ Error: $_" -ForegroundColor Red
            $errorCount++
        }
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Installation Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Installed: $installedCount" -ForegroundColor Green
Write-Host "Skipped: $skippedCount" -ForegroundColor Yellow
Write-Host "Errors: $errorCount" -ForegroundColor $(if ($errorCount -gt 0) { "Red" } else { "Green" })
Write-Host ""

if ($errorCount -eq 0 -and $installedCount -gt 0) {
    Write-Host "✓ Installation completed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "1. Start Revit" -ForegroundColor White
    Write-Host "2. The add-in will load automatically" -ForegroundColor White
    Write-Host "3. Use MCP tools to execute commands" -ForegroundColor White
    Write-Host ""
    Write-Host "Configuration file: %LOCALAPPDATA%\RevitCommandRunner\config.json" -ForegroundColor Gray
    Write-Host "Command queue: %LOCALAPPDATA%\RevitCommandRunner\command-queue.json" -ForegroundColor Gray
    Write-Host ""
}
elseif ($installedCount -eq 0 -and $skippedCount -gt 0) {
    Write-Host "⚠ No Revit versions found or no builds available." -ForegroundColor Yellow
    Write-Host "  Make sure Revit is installed and Build-AllVersions.ps1 has been run." -ForegroundColor Yellow
}
else {
    Write-Host "⚠ Installation completed with errors." -ForegroundColor Yellow
    Write-Host "  Check the output above for details." -ForegroundColor Yellow
}

Write-Host ""
