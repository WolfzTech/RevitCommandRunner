# RevitCommandRunner - Getting Started

## Overview

RevitCommandRunner is a framework that enables AI agents (like Claude Code, Cursor, etc.) to execute Revit commands programmatically with hot-reload support. No Revit restart needed between code changes.

## How It Works

```
AI Agent → command-queue.json → Revit (via ExternalEvent) → results-{id}.json → AI Agent
```

1. AI agent writes a command request to `command-queue.json`
2. RevitCommandRunner monitors the file and triggers execution via ExternalEvent
3. Command executes with proper transaction context
4. Results (success/failure, logs, exceptions, custom data) are written to `results-{id}.json`
5. AI agent reads the result and decides next steps (fix code, rebuild, retry, etc.)

## Installation

### 1. Build the Framework

```powershell
cd D:\RevitCommandRunner
dotnet build src\RevitCommandRunner\RevitCommandRunner.csproj
```

### 2. Install the Add-in

The add-in manifest is already created at:
```
C:\ProgramData\Autodesk\Revit\Addins\2025\RevitCommandRunner.addin
```

### 3. Start Revit

When Revit starts, you'll see a dialog: "Command runner started. Monitoring: ..."

The framework is now active and monitoring for commands.

## Usage for AI Agents

### PowerShell Module (Recommended)

```powershell
# Import the module
Import-Module D:\RevitCommandRunner\tools\RevitCommandRunner.psm1

# Execute a command
$result = Invoke-RevitCommand `
    -DllPath "D:\MyPlugin\bin\Debug\MyPlugin.dll" `
    -CommandClassName "MyPlugin.VerifyCommand" `
    -Args @("arg1", "arg2")

# Check result
if ($result.success) {
    Write-Host "✓ Command succeeded: $($result.message)"
    Write-Host "Execution time: $($result.executionTimeMs)ms"
    
    # Access custom data if command implements ICommandWithData
    if ($result.customData) {
        Write-Host "Custom data: $($result.customData | ConvertTo-Json)"
    }
} else {
    Write-Host "✗ Command failed: $($result.message)"
    
    if ($result.exception) {
        Write-Host "Exception: $($result.exception.type)"
        Write-Host $result.exception.message
        Write-Host $result.exception.stackTrace
    }
}

# View logs
$result.logs | ForEach-Object { Write-Host $_ }
```

### Manual JSON (Alternative)

```powershell
# 1. Create command request
$request = @{
    id = "run-001"
    timestamp = (Get-Date).ToUniversalTime().ToString("o")
    dll = "D:\MyPlugin\bin\Debug\MyPlugin.dll"
    command = "MyPlugin.VerifyCommand"
    args = @("arg1", "arg2")
    metadata = @
} | ConvertTo-Json

# 2. Write to queue
$request | Set-Content "$env:LOCALAPPDATA\RevitCommandRunner\command-queue.json"

# 3. Wait for result
$resultPath = "$env:LOCALAPPDATA\RevitCommandRunner\results\results-run-001.json"
while (-not (Test-Path $resultPath)) { Start-Sleep -Milliseconds 500 }

# 4. Read result
$result = Get-Content $resultPath | ConvertFrom-Json
```

## Command Request Format

```json
{
  "id": "run-001",
  "timestamp": "2026-05-14T10:30:00.000Z",
  "dll": "D:\\MyPlugin\\bin\\Debug\\MyPlugin.dll",
  "command": "MyPlugin.VerifyCommand",
  "args": ["arg1", "arg2"],
  "metadata": {
    "source": "Claude Code",
    "iteration": 1
  }
}
```

## Command Result Format

```json
{
  "id": "run-001",
  "success": true,
  "result": "Succeeded",
  "message": "Verification completed",
  "executionTimeMs": 1234,
  "timestamp": "2026-05-14T10:30:01.234Z",
  "logs": [
    "Starting verification...",
    "Found 10 elements",
    "Verification passed"
  ],
  "exception": null,
  "customData": {
    "passCount": 10,
    "failCount": 0
  }
}
```

## Returning Custom Data from Commands

Commands can implement `ICommandWithData` to return structured data:

```csharp
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitCommandRunner.Core;

public class MyCommand : IExternalCommandWithUIApp, ICommandWithData
{
    private Dictionary<string, object> _customData = new Dictionary<string, object>();

    public Result Execute(UIApplication app, ref string message, ElementSet elements)
    {
        var doc = app.ActiveUIDocument.Document;
        
        // Your command logic
        int passCount = 10;
        int failCount = 2;

        // Store custom data
        _customData["passCount"] = passCount;
        _customData["failCount"] = failCount;
        _customData["details"] = new { foo = "bar" };

        message = $"Verification: {passCount} passed, {failCount} failed";
        return Result.Succeeded;
    }

    public Dictionary<string, object> GetCustomData()
    {
        return _customData;
    }
}
```

## Command Interfaces

RevitCommandRunner supports two command interfaces:

### IExternalCommandWithUIApp (Recommended)

This interface receives `UIApplication` directly, bypassing ExternalCommandData:

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
        
        message = "Command completed successfully";
        return Result.Succeeded;
    }
}
```

**When to use:**
- New commands written specifically for RevitCommandRunner
- When you need guaranteed compatibility
- When you don't need ExternalCommandData's View property

**Advantages:**
- Direct UIApplication access
- No reflection complexity
- Reliable execution

### IExternalCommand (Standard Revit)

The standard Revit command interface:

```csharp
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

public class MyCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var app = commandData.Application;
        var doc = commandData.Application.ActiveUIDocument.Document;
        var view = commandData.View;  // Active view from ExternalCommandData
        
        // Your command logic here
        
        return Result.Succeeded;
    }
}
```

**When to use:**
- Existing commands you want to run via RevitCommandRunner
- When you need the View property from ExternalCommandData

**Note:** The framework attempts to create ExternalCommandData via reflection, but this may fail due to Revit API internal constructor limitations. If you encounter "Failed to create ExternalCommandData" errors, switch to `IExternalCommandWithUIApp`.

## Dependencies and Hot-Reload

Commands with external dependencies (NuGet packages like Xbim.Ifc, other DLLs) work automatically:

- The framework copies your command DLL to a temp location for hot-reload
- Dependencies are automatically resolved from your original DLL's directory
- You can rebuild and re-run without restarting Revit
- No manual dependency copying needed

**Example with dependencies:**
```csharp
using Xbim.Ifc;  // External NuGet package
using RevitCommandRunner.Core;

public class ImportIfcCommand : IExternalCommandWithUIApp
{
    public Result Execute(UIApplication app, ref string message, ElementSet elements)
    {
        // Xbim.Ifc types work automatically
        using var model = IfcStore.Open("path.ifc");
        // ... your logic
        return Result.Succeeded;
    }
}
```

## AI Development Loop Example

```powershell
# Import module
Import-Module D:\RevitCommandRunner\tools\RevitCommandRunner.psm1

# Loop until all issues resolved
$iteration = 1
$allPassed = $false

while (-not $allPassed) {
    Write-Host "`n=== Iteration $iteration ==="
    
    # 1. Build the plugin
    dotnet build D:\MyPlugin\MyPlugin.csproj
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed. Fix compilation errors first."
        break
    }
    
    # 2. Execute command in Revit
    $result = Invoke-RevitCommand `
        -DllPath "D:\MyPlugin\bin\Debug\net8.0-windows\MyPlugin.dll" `
        -CommandClassName "MyPlugin.VerifyCommand"
    
    # 3. Analyze results
    if ($result.success -and $result.customData.failCount -eq 0) {
        Write-Host "✓ All tests passed!"
        $allPassed = $true
    } else {
        Write-Host "✗ Issues found:"
        Write-Host "  Pass: $($result.customData.passCount)"
        Write-Host "  Fail: $($result.customData.failCount)"
        Write-Host "  Message: $($result.message)"
        
        # 4. AI analyzes logs and fixes code
        # (This is where Claude Code would read the result and fix the code)
        
        $iteration++
    }
}
```

## Configuration

Default configuration is stored at:
```
%LOCALAPPDATA%\RevitCommandRunner\config.json
```

You can customize:
- `queueFilePath` - Where to monitor for commands
- `resultsDirectory` - Where to write results
- `maxExecutionTimeSeconds` - Timeout (default: 300)
- `captureConsoleLogs` - Capture Console.WriteLine output (default: true)
- `deleteQueueFileAfterRead` - Auto-delete queue file (default: true)

## Hot-Reload

The framework copies your DLL to a temp location before loading it. This means:
- ✓ You can rebuild your plugin without closing Revit
- ✓ Each execution loads the latest version
- ✓ No file locking issues

Temp DLLs are stored in: `%TEMP%\RevitCommandRunner\`

## Troubleshooting

### Command not executing
- Check Revit is running and the add-in loaded successfully
- Verify the queue file path: `%LOCALAPPDATA%\RevitCommandRunner\command-queue.json`
- Look for error files in: `%LOCALAPPDATA%\RevitCommandRunner\results\error-*.txt`

### DLL not found
- Ensure the DLL path in the request is absolute and correct
- Check the DLL was built successfully

### Transaction errors
- Commands execute via ExternalEvent, which provides proper transaction context
- If you need a transaction, create one inside your Execute method

### Timeout
- Default timeout is 5 minutes
- Increase via `Invoke-RevitCommand -TimeoutSeconds 600`
- Or update the config file

## Next Steps

- See `examples.md` for more usage examples
- See `api-reference.md` for detailed API documentation
- See `PLAN.md` for the full project architecture and roadmap
