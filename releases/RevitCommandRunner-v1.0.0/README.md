# RevitCommandRunner - README

AI-driven command execution framework for Autodesk Revit with hot-reload support.

## What is this?

RevitCommandRunner enables AI agents (Claude Code, Cursor, etc.) to execute Revit commands programmatically without restarting Revit. It's like "MCP + Revit Add-in Manager" — AI can execute commands, gather results, fix code, rebuild, and run in a loop until all issues are resolved.

## Key Features

- ✅ **Hot-reload**: Rebuild your plugin without closing Revit
- ✅ **AI-friendly**: File-based JSON communication works with any AI agent
- ✅ **Proper transaction context**: Uses ExternalEvent for safe command execution
- ✅ **Rich results**: Captures logs, exceptions, timing, and custom data
- ✅ **Generic**: Works with any IExternalCommand, not just specific commands
- ✅ **Standalone**: Reusable framework for future projects

## Quick Start

### 1. Install

**Option A: Using Installer.exe (Recommended)**

1. Download and extract `RevitCommandRunner-v1.0.0.zip`
2. Run `Installer.exe`
3. Start Revit - the add-in loads automatically

**Option B: Build from Source**

```powershell
# Clone or download the repository
cd <YOUR_PATH>\RevitCommandRunner

# Build all Revit versions (2021-2027)
cd src\RevitCommandRunner
.\Build-AllVersions.ps1 -Configuration Release

# Install
cd ..\..\installer
.\Install.ps1
```

The add-in will be installed to:
`%APPDATA%\Autodesk\ApplicationPlugins\RevitCommandRunner.bundle`

### 2. Setup MCP Server (for AI Integration)

The MCP server is included in the bundle - no separate build needed!

### 3. Configure Your AI Agent

**Option A: MCP Server (Recommended for AI agents)**

**For OpenCode:**
Add to `%USERPROFILE%\.config\opencode\opencode.jsonc`:
```json
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
```

**For Claude Desktop:**
Add to `%APPDATA%\Claude\claude_desktop_config.json`:
```json
{
  "mcpServers": {
    "revit-command-runner": {
      "command": "node",
      "args": ["%APPDATA%/Autodesk/ApplicationPlugins/RevitCommandRunner.bundle/mcp-server/index.js"]
    }
  }
}
```

**For Antigravity:**
Add to `%USERPROFILE%\.config\antigravity\config.json`:
```json
{
  "mcpServers": {
    "revit-command-runner": {
      "command": "node",
      "args": ["%APPDATA%/Autodesk/ApplicationPlugins/RevitCommandRunner.bundle/mcp-server/index.js"]
    }
  }
}
```

**Note**: The MCP server is bundled with the installer. After running `Installer.exe`, just configure your AI agent with the path above.

**Note**: OpenCode and Claude Desktop use different config formats. Restart your AI agent after config changes.

**Troubleshooting**: If you see errors, check [mcp-server/TROUBLESHOOTING.md](mcp-server/TROUBLESHOOTING.md).

See [mcp-server/README.md](mcp-server/README.md) for detailed MCP configuration and usage.

**Option B: PowerShell Module (Direct scripting)**

```powershell
# Import the PowerShell module (adjust path to your installation)
Import-Module <YOUR_PATH>\RevitCommandRunner\tools\RevitCommandRunner.psm1

# Execute your command
$result = Invoke-RevitCommand `
    -DllPath "C:\MyPlugin\bin\Debug\MyPlugin.dll" `
    -CommandClassName "MyPlugin.MyCommand"

# Check result
if ($result.success) {
    Write-Host "✓ Success: $($result.message)"
} else {
    Write-Host "✗ Failed: $($result.message)"
}
```

### 4. AI Development Loop

```powershell
while ($true) {
    # Build
    dotnet build MyPlugin.csproj
    
    # Execute in Revit
    $result = Invoke-RevitCommand -DllPath "..." -CommandClassName "..."
    
    # Analyze
    if ($result.success -and $result.customData.failCount -eq 0) {
        Write-Host "✓ All tests passed!"
        break
    }
    
    # AI fixes code based on result.logs and result.exception
    # ... then loop continues
}
```

## How It Works

```
┌─────────────┐         ┌──────────────────┐         ┌────────────┐
│  AI Agent   │────────▶│ command-queue    │────────▶│   Revit    │
│ (OpenCode/  │  write  │     .json        │ monitor │ (External  │
│  Claude)    │         │                  │         │   Event)   │
└─────────────┘         └──────────────────┘         └────────────┘
       ▲                                                     │
       │                                                     │
       │                ┌──────────────────┐                │
       └────────────────│ results-{id}     │◀───────────────┘
            read        │     .json        │    write
                        └──────────────────┘
```

1. AI writes command request to `command-queue.json`
2. RevitCommandRunner monitors the file via FileSystemWatcher
3. Command executes via ExternalEvent (proper transaction context)
4. Results written to `results-{id}.json` with logs, exceptions, custom data
5. AI reads result and decides next steps

## Architecture

- **CommandQueueMonitor** (IExternalApplication) - Monitors queue file, triggers execution
- **CommandExecutor** (IExternalEventHandler) - Loads DLL, executes command, captures results
- **LogCapture** - Captures Console.WriteLine output during execution
- **AssemblyLoader** - Hot-reload via temp DLL copies with timestamps
- **MCP Server** - Model Context Protocol interface for AI agents
- **PowerShell Module** - Convenience functions for direct scripting

## Documentation

- [MCP Server Guide](mcp-server/README.md) - Using with Claude Desktop and other MCP clients
- [Getting Started Guide](docs/getting-started.md) - Installation and basic usage
- [API Reference](docs/api-reference.md) - Detailed API documentation
- [Examples](docs/examples.md) - Common usage patterns
- [PLAN.md](PLAN.md) - Full project architecture and roadmap

## Requirements

- Revit 2025
- .NET 8.0
- Windows
- Node.js 18+ (for MCP server)

## Project Structure

```
RevitCommandRunner/
├── src/
│   └── RevitCommandRunner/
│       ├── Core/                    # Core execution classes
│       │   ├── CommandQueueMonitor.cs
│       │   ├── CommandExecutor.cs
│       │   └── LogCapture.cs
│       ├── Models/                  # Data models
│       │   ├── CommandRequest.cs
│       │   ├── CommandResult.cs
│       │   ├── ExceptionInfo.cs
│       │   └── RevitCommandRunnerConfig.cs
│       └── Utils/                   # Utilities
│           ├── AssemblyLoader.cs
│           └── FileSystemHelper.cs
├── mcp-server/                      # MCP server for AI agents
│   ├── src/
│   │   └── index.ts                 # MCP server implementation
│   ├── package.json
│   └── README.md
├── tools/
│   └── RevitCommandRunner.psm1     # PowerShell module
├── docs/
│   ├── getting-started.md
│   ├── api-reference.md
│   └── examples.md
└── PLAN.md                          # Project plan and architecture
```

## Example: Returning Custom Data

Commands can implement `ICommandWithData` to return structured data alongside the standard Result:

```csharp
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitCommandRunner.Core;

public class VerifyCommand : IExternalCommandWithUIApp, ICommandWithData
{
    private Dictionary<string, object> _data = new Dictionary<string, object>();

    public Result Execute(UIApplication app, ref string message, ElementSet elements)
    {
        var doc = app.ActiveUIDocument.Document;
        
        int passCount = 10;
        int failCount = 2;

        _data["passCount"] = passCount;
        _data["failCount"] = failCount;

        message = $"{passCount} passed, {failCount} failed";
        return failCount == 0 ? Result.Succeeded : Result.Failed;
    }

    public Dictionary<string, object> GetCustomData() => _data;
}
```

## Command Interfaces

RevitCommandRunner supports two command interfaces:

### IExternalCommandWithUIApp (Recommended)

```csharp
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitCommandRunner.Core;

public class MyCommand : IExternalCommandWithUIApp
{
    public Result Execute(UIApplication app, ref string message, ElementSet elements)
    {
        var doc = app.ActiveUIDocument.Document;
        // Your command logic here
        return Result.Succeeded;
    }
}
```

**Advantages:**
- Direct access to UIApplication
- No reflection complexity
- Reliable and straightforward

### IExternalCommand (Standard Revit)

```csharp
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

public class MyCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var app = commandData.Application;
        var doc = commandData.Application.ActiveUIDocument.Document;
        // Your command logic here
        return Result.Succeeded;
    }
}
```

**Note:** The framework attempts to create ExternalCommandData via reflection, but this may fail due to Revit API limitations. Use `IExternalCommandWithUIApp` for guaranteed compatibility.

## Uninstallation

**Option 1: From Windows Settings**
- Open Settings → Apps → Installed apps
- Search for "RevitCommandRunner"
- Click Uninstall

**Option 2: Run uninstaller**
```powershell
.\Installer.exe --uninstall
```

**Option 3: Manual removal**
```
%APPDATA%\Autodesk\ApplicationPlugins\RevitCommandRunner.bundle
```

Commands with external dependencies (NuGet packages, other DLLs) work automatically. The framework resolves dependencies from your command DLL's original directory, so hot-reload doesn't break dependency loading.

## License

MIT

## Credits

Inspired by [ricaun.RevitTest](https://github.com/ricaun-io/ricaun.RevitTest) - test runner architecture adapted for AI-driven command execution.
