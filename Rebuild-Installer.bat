@echo off
setlocal

if not "%REBUILD_INSTALLER_KEEP_OPEN%"=="1" (
  set "REBUILD_INSTALLER_KEEP_OPEN=1"
  cmd /k call "%~f0" %*
  exit /b
)

set "ROOT=%~dp0"
set "VERSION=%~1"
if "%VERSION%"=="" set "VERSION=1.0.2"

echo.
echo ========================================
echo RevitCommandRunner Rebuild Installer
echo ========================================
echo Root: %ROOT%
echo Version: %VERSION%
echo.

where pwsh >nul 2>nul
if errorlevel 1 (
  echo [ERROR] pwsh PowerShell 7+ is not installed or not on PATH.
  echo.
  pause
  exit /b 1
)

where dotnet >nul 2>nul
if errorlevel 1 (
  echo [ERROR] dotnet is not installed or not on PATH.
  echo.
  pause
  exit /b 1
)

where node >nul 2>nul
if errorlevel 1 (
  echo [ERROR] node is not installed or not on PATH.
  echo.
  pause
  exit /b 1
)

where npm >nul 2>nul
if errorlevel 1 (
  echo [ERROR] npm is not installed or not on PATH.
  echo.
  pause
  exit /b 1
)

echo [1/3] Building Revit bundle...
pwsh -NoProfile -ExecutionPolicy Bypass -File "%ROOT%src\RevitCommandRunner\Build-AllVersions.ps1" -Configuration Release
if errorlevel 1 (
  echo [ERROR] Build-AllVersions.ps1 failed.
  echo.
  pause
  exit /b 1
)

echo [2/3] Building MCP server...
pushd "%ROOT%mcp-server"
call npm install
if errorlevel 1 (
  popd
  echo [ERROR] npm install failed.
  echo.
  pause
  exit /b 1
)
call npm run build
if errorlevel 1 (
  popd
  echo [ERROR] npm run build failed.
  echo.
  pause
  exit /b 1
)
popd

echo [3/3] Building embedded installer...
pwsh -NoProfile -ExecutionPolicy Bypass -File "%ROOT%installer\Create-Embedded-Installer.ps1" -Version "%VERSION%"
if errorlevel 1 (
  echo [ERROR] Create-Embedded-Installer.ps1 failed.
  echo.
  pause
  exit /b 1
)

echo.
echo ========================================
echo Build complete
echo ========================================
echo Output: %ROOT%releases\RevitCommandRunner-v%VERSION%-Installer.exe
echo.
pause
exit /b 0
