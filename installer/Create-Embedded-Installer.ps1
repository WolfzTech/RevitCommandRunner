# Create Self-Contained Installer with Embedded Bundle
# Embeds the bundle as a ZIP resource inside the installer executable.

param(
    [string]$Version = "1.0.0",
    [string]$OutputDir = "..\releases"
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "RevitCommandRunner Embedded Installer" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$buildPath = Join-Path $PSScriptRoot "..\build\RevitCommandRunner.bundle"
$installerProjectPath = Join-Path $PSScriptRoot "RevitCommandRunnerInstaller"
$outputPath = Join-Path $PSScriptRoot $OutputDir
$installerExe = "RevitCommandRunner-v$Version-Installer.exe"
$installerOutput = Join-Path $outputPath $installerExe

if (-not (Test-Path $buildPath)) {
    Write-Host "Build not found: $buildPath" -ForegroundColor Red
    Write-Host "Please run Build-AllVersions.ps1 first." -ForegroundColor Yellow
    exit 1
}

if (-not (Test-Path $outputPath)) {
    New-Item -ItemType Directory -Path $outputPath | Out-Null
}

Write-Host "Creating self-contained installer..." -ForegroundColor Yellow
Write-Host "  Version: $Version" -ForegroundColor Gray
Write-Host ""

Write-Host "Step 1: Staging bundle..." -ForegroundColor Yellow
$stagingDir = Join-Path $PSScriptRoot "staging"
if (Test-Path $stagingDir) {
    Remove-Item -Path $stagingDir -Recurse -Force
}
New-Item -ItemType Directory -Path $stagingDir | Out-Null

Copy-Item -Path $buildPath -Destination $stagingDir -Recurse -Force
Copy-Item -Path "$PSScriptRoot\PackageContents.xml" -Destination (Join-Path $stagingDir "RevitCommandRunner.bundle\PackageContents.xml") -Force

Write-Host "Step 2: Adding MCP server..." -ForegroundColor Yellow
$mcpSourceRoot = Join-Path $PSScriptRoot "..\mcp-server"
$mcpDest = Join-Path $stagingDir "RevitCommandRunner.bundle\mcp-server"
if (Test-Path "$mcpSourceRoot\dist") {
    New-Item -ItemType Directory -Path $mcpDest -Force | Out-Null
    Copy-Item -Path "$mcpSourceRoot\dist\*" -Destination $mcpDest -Recurse -Force

    if (Test-Path "$mcpSourceRoot\node_modules") {
        Copy-Item -Path "$mcpSourceRoot\node_modules" -Destination $mcpDest -Recurse -Force
    }
    else {
        Write-Host "  Warning: node_modules not found. Run 'npm install' in mcp-server directory." -ForegroundColor Yellow
    }

    if (Test-Path "$mcpSourceRoot\package.json") {
        Copy-Item -Path "$mcpSourceRoot\package.json" -Destination $mcpDest -Force
    }
}
else {
    Write-Host "  Warning: MCP server not built. Run 'npm run build' in mcp-server directory." -ForegroundColor Yellow
}

Write-Host "Step 3: Creating bundle ZIP..." -ForegroundColor Yellow
$bundleZip = Join-Path $stagingDir "bundle.zip"
Compress-Archive -Path "$stagingDir\RevitCommandRunner.bundle" -DestinationPath $bundleZip -CompressionLevel Optimal

$zipSize = [math]::Round((Get-Item $bundleZip).Length / 1MB, 2)
Write-Host "  Bundle ZIP size: $zipSize MB" -ForegroundColor Gray

Write-Host "Step 4: Embedding bundle in installer..." -ForegroundColor Yellow
$resourceDir = Join-Path $installerProjectPath "Resources"
if (-not (Test-Path $resourceDir)) {
    New-Item -ItemType Directory -Path $resourceDir | Out-Null
}
Copy-Item -Path $bundleZip -Destination (Join-Path $resourceDir "bundle.zip") -Force

$csprojPath = Join-Path $installerProjectPath "RevitCommandRunnerInstaller.csproj"
$csproj = Get-Content $csprojPath -Raw
if ($csproj -notmatch '<EmbeddedResource Include="Resources\\bundle.zip"') {
    $resourceXml = @"
  <ItemGroup>
    <EmbeddedResource Include="Resources\bundle.zip" />
  </ItemGroup>
"@
    $csproj = $csproj -replace '</Project>', "$resourceXml`n</Project>"
    Set-Content -Path $csprojPath -Value $csproj -NoNewline
}

try {
    Write-Host "Step 5: Building installer..." -ForegroundColor Yellow
    dotnet publish $installerProjectPath -c Release -r win-x64 --self-contained true -o "$PSScriptRoot\bin" | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed" -ForegroundColor Red
        exit 1
    }

    Copy-Item -Path "$PSScriptRoot\bin\RevitCommandRunnerInstaller.exe" -Destination $installerOutput -Force
}
finally {
    if (Test-Path $stagingDir) {
        Remove-Item -Path $stagingDir -Recurse -Force -ErrorAction SilentlyContinue
    }
    if (Test-Path $resourceDir) {
        Remove-Item -Path $resourceDir -Recurse -Force -ErrorAction SilentlyContinue
    }

    $csproj = Get-Content $csprojPath -Raw
    $csproj = $csproj -replace '\s*<ItemGroup>\s*<EmbeddedResource Include="Resources\\bundle.zip" />\s*</ItemGroup>\s*', ''
    Set-Content -Path $csprojPath -Value $csproj -NoNewline
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Self-Contained Installer Created" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host "File: $installerExe" -ForegroundColor Cyan
$finalSize = [math]::Round((Get-Item $installerOutput).Length / 1MB, 2)
Write-Host "Size: $finalSize MB" -ForegroundColor Gray
Write-Host ""
Write-Host "Distribution Instructions:" -ForegroundColor Yellow
Write-Host "  1. Distribute ONLY: $installerExe" -ForegroundColor White
Write-Host "  2. Users run the installer" -ForegroundColor White
Write-Host "  3. Bundle extracts automatically" -ForegroundColor White
Write-Host "  4. Registers in Windows Programs & Features" -ForegroundColor White
Write-Host ""
Write-Host "Single-file installer ready!" -ForegroundColor Green
Write-Host ""
