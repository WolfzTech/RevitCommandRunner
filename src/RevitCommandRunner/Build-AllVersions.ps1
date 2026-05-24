# Build script for RevitCommandRunner - All Revit versions (2021-2027)
# Builds separate DLLs for each Revit version with appropriate .NET framework

param(
    [string]$Configuration = "Release",
    [string]$OutputDir = "..\..\build"
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "RevitCommandRunner Multi-Version Build" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Define Revit versions and their configurations
$revitVersions = @(
    @{ Year = "2021"; Framework = "net48"; ApiVersion = "2021.*" },
    @{ Year = "2022"; Framework = "net48"; ApiVersion = "2022.*" },
    @{ Year = "2023"; Framework = "net48"; ApiVersion = "2023.*" },
    @{ Year = "2024"; Framework = "net48"; ApiVersion = "2024.*" },
    @{ Year = "2025"; Framework = "net8.0-windows"; ApiVersion = "2025.*" },
    @{ Year = "2026"; Framework = "net8.0-windows"; ApiVersion = "2026.*" },
    @{ Year = "2027"; Framework = "net10.0-windows"; ApiVersion = "2027.*" }
)

# Create output directory
$buildRoot = Join-Path $PSScriptRoot $OutputDir
if (Test-Path $buildRoot) {
    Remove-Item -Path $buildRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $buildRoot | Out-Null

$projectFile = Join-Path $PSScriptRoot "RevitCommandRunner.csproj"
$successCount = 0
$failCount = 0

foreach ($version in $revitVersions) {
    $year = $version.Year
    $framework = $version.Framework
    $apiVersion = $version.ApiVersion
    
    Write-Host "Building for Revit $year..." -ForegroundColor Yellow
    Write-Host "  Framework: $framework" -ForegroundColor Gray
    Write-Host "  API Version: $apiVersion" -ForegroundColor Gray
    
    # Create version-specific output directory
    $versionOutput = Join-Path $buildRoot "Revit$year"
    New-Item -ItemType Directory -Path $versionOutput | Out-Null
    
    # Temporarily modify project file to include Revit API references
    $projectContent = Get-Content $projectFile -Raw
    $originalContent = $projectContent
    
    # Remove existing closing tag and add version-specific references
    $apiReferences = @"

  <!-- Revit $year API References -->
  <ItemGroup Condition="'`$(TargetFramework)' == '$framework'">
    <PackageReference Include="Nice3point.Revit.Api.RevitAPI" Version="$apiVersion" />
    <PackageReference Include="Nice3point.Revit.Api.RevitAPIUI" Version="$apiVersion" />
  </ItemGroup>

</Project>
"@
    
    $modifiedContent = $projectContent -replace '</Project>', $apiReferences
    Set-Content -Path $projectFile -Value $modifiedContent -NoNewline
    
    try {
        # Build for specific framework
        $buildOutput = & dotnet build $projectFile `
            --configuration $Configuration `
            --framework $framework `
            --output $versionOutput `
            --verbosity minimal 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  ✓ Build succeeded" -ForegroundColor Green
            $successCount++
            
            # Copy to bundle structure
            $bundleDir = Join-Path $buildRoot "RevitCommandRunner.bundle\Contents"
            $yearDir = Join-Path $bundleDir $year
            New-Item -ItemType Directory -Path $yearDir -Force | Out-Null
            
            Copy-Item -Path "$versionOutput\RevitCommandRunner.dll" -Destination $yearDir
            Copy-Item -Path "$versionOutput\RevitCommandRunner.pdb" -Destination $yearDir -ErrorAction SilentlyContinue
            Copy-Item -Path "$versionOutput\Newtonsoft.Json.dll" -Destination $yearDir -ErrorAction SilentlyContinue

            $manifestSource = Join-Path $PSScriptRoot "..\..\installer\RevitCommandRunner.addin"
            if (Test-Path $manifestSource) {
                Copy-Item -Path $manifestSource -Destination $yearDir -Force
            }
        }
        else {
            Write-Host "  ✗ Build failed" -ForegroundColor Red
            Write-Host $buildOutput -ForegroundColor Red
            $failCount++
        }
    }
    catch {
        Write-Host "  ✗ Build error: $_" -ForegroundColor Red
        $failCount++
    }
    finally {
        # Restore original project file
        Set-Content -Path $projectFile -Value $originalContent -NoNewline
    }
    
    Write-Host ""
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Build Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Successful: $successCount" -ForegroundColor Green
Write-Host "Failed: $failCount" -ForegroundColor $(if ($failCount -gt 0) { "Red" } else { "Green" })
Write-Host "Output: $buildRoot" -ForegroundColor Cyan
Write-Host ""

if ($failCount -eq 0) {
    Write-Host "✓ All builds completed successfully!" -ForegroundColor Green
    exit 0
}
else {
    Write-Host "✗ Some builds failed. Check output above." -ForegroundColor Red
    exit 1
}
