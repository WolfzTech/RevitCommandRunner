# Sample Plugin - RevitCommandRunner Demo

This sample demonstrates how to create Revit commands that work with RevitCommandRunner and can be executed by AI agents via MCP.

## What's Included

### 1. HelloRevitCommand
A simple read-only command that gathers information about the active Revit document.

**Features:**
- No document modifications
- Returns document info (title, path, element counts)
- Demonstrates custom data return via `ICommandWithData`
- Safe to run anytime

**Custom Data Returned:**
```json
{
  "hasDocument": true,
  "documentTitle": "Project1",
  "documentPath": "",
  "isModified": false,
  "totalElements": 6078,
  "wallCount": 0,
  "levelCount": 2,
  "revitVersion": "2025"
}
```

### 2. CreateWallsCommand
A command that creates 3 walls in the active document.

**Features:**
- Creates walls using transactions
- Demonstrates proper error handling
- Returns success/failure counts
- Uses Console.WriteLine for logging

**Custom Data Returned:**
```json
{
  "wallsCreated": 3,
  "wallsFailed": 0,
  "documentTitle": "Project1"
}
```

## Project Structure

```
samples/SamplePlugin/
├── SamplePlugin.csproj          # Project file
├── HelloRevitCommand.cs         # Read-only info command
└── CreateWallsCommand.cs        # Wall creation command
```

## Building the Sample

```bash
cd D:\RevitCommandRunner\samples\SamplePlugin
dotnet build
```

Output: `bin\Debug\net8.0-windows\SamplePlugin.dll`

## Running via AI Agent (OpenCode/Claude)

Once the MCP server is configured, simply ask:

```
Execute HelloRevitCommand from the sample plugin
```

Or more explicitly:

```
Execute the command:
- DLL: D:\RevitCommandRunner\samples\SamplePlugin\bin\Debug\net8.0-windows\SamplePlugin.dll
- Class: SamplePlugin.HelloRevitCommand
```

## Running via PowerShell

```powershell
Import-Module D:\RevitCommandRunner\tools\RevitCommandRunner.psm1

$result = Invoke-RevitCommand `
    -DllPath "D:\RevitCommandRunner\samples\SamplePlugin\bin\Debug\net8.0-windows\SamplePlugin.dll" `
    -CommandClassName "SamplePlugin.HelloRevitCommand"

Write-Host "Success: $($result.success)"
Write-Host "Message: $($result.message)"
Write-Host "Custom Data:"
$result.customData | ConvertTo-Json
```

## Key Concepts Demonstrated

### 1. Using IExternalCommandWithUIApp

```csharp
public class HelloRevitCommand : IExternalCommandWithUIApp, ICommandWithData
{
    public Result Execute(UIApplication app, ref string message, ElementSet elements)
    {
        var doc = app.ActiveUIDocument.Document;
        // Your code here
        return Result.Succeeded;
    }
}
```

**Why IExternalCommandWithUIApp?**
- Direct access to UIApplication
- No reflection complexity
- Guaranteed compatibility with RevitCommandRunner

### 2. Returning Custom Data

```csharp
private Dictionary<string, object> _customData = new Dictionary<string, object>();

public Dictionary<string, object> GetCustomData()
{
    return _customData;
}
```

Custom data is returned in the JSON result and accessible to AI agents.

### 3. Logging with Console.WriteLine

```csharp
Console.WriteLine("[Info] Starting command");
Console.WriteLine($"[OK] Created wall {i + 1}");
Console.WriteLine($"[Error] Failed: {ex.Message}");
```

All console output is captured in the `logs` array of the result.

### 4. Proper Transaction Handling

```csharp
using (Transaction trans = new Transaction(doc, "Create Sample Walls"))
{
    trans.Start();
    try
    {
        // Modify document
        trans.Commit();
    }
    catch (Exception ex)
    {
        trans.RollBack();
        return Result.Failed;
    }
}
```

## Example AI Workflow

### 1. Check Document Status
```
Execute HelloRevitCommand to see what's in the document
```

**Result:**
- Document: Project1
- Elements: 6078
- Walls: 0
- Levels: 2

### 2. Create Walls
```
Execute CreateWallsCommand to add some walls
```

**Result:**
- Created 3 walls successfully
- 0 failures

### 3. Verify Changes
```
Execute HelloRevitCommand again to verify the walls were created
```

**Result:**
- Document: Project1
- Elements: 6081 (+3)
- Walls: 3 (+3)
- Document modified: true

## Creating Your Own Commands

### Template

```csharp
using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitCommandRunner.Core;

namespace YourNamespace
{
    public class YourCommand : IExternalCommandWithUIApp, ICommandWithData
    {
        private Dictionary<string, object> _customData = new Dictionary<string, object>();

        public Result Execute(UIApplication app, ref string message, ElementSet elements)
        {
            try
            {
                Console.WriteLine("[Info] Starting YourCommand");
                
                var doc = app.ActiveUIDocument.Document;
                
                // Your logic here
                
                _customData["someKey"] = "someValue";
                
                message = "Success!";
                Console.WriteLine("[Success] Command completed");
                
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = $"Error: {ex.Message}";
                Console.WriteLine($"[Error] {ex.Message}");
                return Result.Failed;
            }
        }

        public Dictionary<string, object> GetCustomData()
        {
            return _customData;
        }
    }
}
```

### Best Practices

1. **Always use try-catch** - Catch exceptions and return meaningful error messages
2. **Log everything** - Use Console.WriteLine for debugging
3. **Return custom data** - Help AI agents understand what happened
4. **Use transactions** - For any document modifications
5. **Check for null** - Verify document, elements exist before using
6. **Meaningful messages** - Set the `message` parameter with useful info

## Testing Your Commands

### 1. Build
```bash
dotnet build YourPlugin.csproj
```

### 2. Test via AI
```
Execute my command:
- DLL: D:\YourPlugin\bin\Debug\net8.0-windows\YourPlugin.dll
- Class: YourNamespace.YourCommand
```

### 3. Check Results
The AI will show you:
- Success/failure status
- Execution time
- All console logs
- Custom data returned
- Any exceptions

### 4. Iterate
If there are errors:
- AI reads the error message and stack trace
- AI fixes the code
- Rebuild and test again
- No need to restart Revit!

## Hot-Reload in Action

The beauty of RevitCommandRunner is **hot-reload**:

1. Make code changes
2. Build (`dotnet build`)
3. Execute command again
4. See results immediately

**No Revit restart needed!** The DLL is copied to a temp location each time, so you can rebuild while Revit is running.

## Troubleshooting

### "No active document"
- Open or create a Revit project first
- Some commands require an active document

### "Transaction already started"
- Don't nest transactions
- Check if a transaction is already active before starting

### "Element not found"
- Verify elements exist before accessing
- Use FilteredElementCollector to find elements

### Command times out
- Increase timeout: `timeoutSeconds: 120`
- Check for infinite loops or blocking operations

## Next Steps

1. **Modify the samples** - Change wall positions, add more elements
2. **Create your own commands** - Use the template above
3. **Test with AI** - Let AI build and test iteratively
4. **Build complex workflows** - Chain multiple commands together

## See Also

- [Main README](../../README.md) - RevitCommandRunner overview
- [MCP Server Guide](../../mcp-server/README.md) - MCP configuration
- [API Reference](../../docs/api-reference.md) - Detailed API docs
