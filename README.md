# RevitCommandRunner

AI-driven command execution framework for Autodesk Revit.

RevitCommandRunner installs as a Revit application plugin and exposes an MCP server so AI agents can execute Revit commands, read results, fix code, rebuild, and run again without restarting Revit.

## Components

- `src/RevitCommandRunner` - Revit add-in source.
- `mcp-server` - Node.js MCP server used by AI clients.
- `installer/RevitCommandRunnerInstaller` - WPF installer EXE source.
- `installer/Create-Embedded-Installer.ps1` - embeds the built bundle and MCP server into one distributable installer.
- `Rebuild-Installer.bat` - one-click release build wrapper.

## Build Installer

Run from the repository root:

```cmd
Rebuild-Installer.bat 1.0.2
```

The output is:

```text
releases\RevitCommandRunner-v1.0.2-Installer.exe
```

Distribute only that EXE. It contains the Revit bundle, MCP server, and dependencies.

## Installer Behavior

The installer places the bundle here:

```text
%APPDATA%\Autodesk\ApplicationPlugins\RevitCommandRunner.bundle
```

After installing the bundle, it shows a WPF checkbox dialog for MCP client configuration. All clients are checked by default:

- Claude Code
- OpenCode
- Antigravity

The installer creates or updates the relevant MCP config entry. Existing unrelated MCP servers are preserved. If `revit-command-runner` already exists, it is updated instead of duplicated.

## MCP Server Path

All clients point to the bundled MCP server:

```text
%APPDATA%/Autodesk/ApplicationPlugins/RevitCommandRunner.bundle/mcp-server/index.js
```

## Requirements

- Windows
- Autodesk Revit 2021-2027
- .NET SDK for building
- Node.js 18+ and npm for building the MCP server
- PowerShell 7+ (`pwsh`) for the release scripts

## Development Build Steps

`Rebuild-Installer.bat` runs these steps:

```cmd
pwsh -NoProfile -ExecutionPolicy Bypass -File src\RevitCommandRunner\Build-AllVersions.ps1 -Configuration Release
cd mcp-server
npm install
npm run build
cd ..
pwsh -NoProfile -ExecutionPolicy Bypass -File installer\Create-Embedded-Installer.ps1 -Version 1.0.2
```

## Runtime Files

The add-in and MCP server communicate through JSON files in:

```text
%LOCALAPPDATA%\RevitCommandRunner
```

Main files:

- `command-queue.json`
- `results\results-{id}.json`

## Command Interface

Commands can implement `IExternalCommandWithUIApp` for direct `UIApplication` access:

```csharp
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitCommandRunner.Core;

public class MyCommand : IExternalCommandWithUIApp
{
    public Result Execute(UIApplication app, ref string message, ElementSet elements)
    {
        var doc = app.ActiveUIDocument.Document;
        message = $"Active document: {doc.Title}";
        return Result.Succeeded;
    }
}
```

Standard `IExternalCommand` is also supported, but `IExternalCommandWithUIApp` is preferred.
