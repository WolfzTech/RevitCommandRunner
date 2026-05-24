# Quick Reference - Using RevitCommandRunner with AI

## How to Use (Simple Version)

### 1. Just Ask Me!

Once configured, you can simply ask:

```
Execute HelloRevitCommand from the sample plugin
```

```
Create 3 walls in Revit
```

```
Check what's in the current Revit document
```

I'll automatically use the MCP tools to execute commands in Revit.

### 2. Explicit Command Execution

If you want to be specific:

```
Execute this command:
- DLL: D:\RevitCommandRunner\samples\SamplePlugin\bin\Debug\net8.0-windows\SamplePlugin.dll
- Class: SamplePlugin.HelloRevitCommand
```

### 3. Build and Test Loop

```
Build the sample plugin and test HelloRevitCommand
```

I'll:
1. Run `dotnet build`
2. Execute the command in Revit
3. Show you the results
4. If there are errors, I can fix them and retry

## Available Sample Commands

### HelloRevitCommand
- **What it does**: Reads document info (elements, walls, levels)
- **Safe**: Read-only, no modifications
- **Use when**: You want to check document status

### CreateWallsCommand
- **What it does**: Creates 3 walls in the document
- **Modifies**: Yes, adds walls
- **Use when**: Testing wall creation or document modifications

## Common Tasks

### Check if Revit is Ready
```
Is Revit ready?
```

### Get Document Info
```
Execute HelloRevitCommand
```

### Create Walls
```
Execute CreateWallsCommand
```

### View Recent Commands
```
Show me the last 5 command executions
```

### Build and Test
```
Build SamplePlugin and run HelloRevitCommand
```

## What You'll See

When a command executes, you'll get:

✅ **Success/Failure** - Did it work?
⏱️ **Execution Time** - How long it took (milliseconds)
📝 **Logs** - All console output from the command
📊 **Custom Data** - Structured data returned by the command
❌ **Exceptions** - Full error details if something failed

## Example Results

### Successful Execution
```json
{
  "success": true,
  "result": "Succeeded",
  "message": "Hello from Revit! Document: Project1, Elements: 6078",
  "executionTimeMs": 53,
  "logs": [
    "[Info] Starting HelloRevitCommand",
    "[Info] Document: Project1",
    "[Success] Command completed"
  ],
  "customData": {
    "totalElements": 6078,
    "wallCount": 0,
    "levelCount": 2
  }
}
```

### Failed Execution
```json
{
  "success": false,
  "result": "Failed",
  "message": "No active document",
  "exception": {
    "type": "System.NullReferenceException",
    "message": "Object reference not set to an instance of an object",
    "stackTrace": "..."
  }
}
```

## AI Development Workflow

### Typical Session

1. **You**: "Build and test my plugin"
2. **AI**: Builds, executes, shows results
3. **You**: "The wall height is wrong, make it 10 feet"
4. **AI**: Edits code, rebuilds, tests again
5. **You**: "Perfect! Now add a door"
6. **AI**: Adds door code, builds, tests
7. Repeat until done!

### No Revit Restart Needed!

The hot-reload feature means:
- Make changes
- Build
- Test immediately
- See results in seconds

## Tips

### For Best Results

1. **Keep Revit open** with a document loaded
2. **Be specific** about what you want to test
3. **Check logs** if something fails - they show exactly what happened
4. **Use custom data** to return structured information to AI

### Common Patterns

**Read then Modify:**
```
First check the document status, then create 5 walls
```

**Iterative Testing:**
```
Build and test until all walls are created successfully
```

**Debugging:**
```
Execute the command and show me the full logs
```

## Creating Your Own Commands

### Quick Template

```csharp
public class MyCommand : IExternalCommandWithUIApp, ICommandWithData
{
    private Dictionary<string, object> _customData = new();

    public Result Execute(UIApplication app, ref string message, ElementSet elements)
    {
        try
        {
            Console.WriteLine("[Info] Starting MyCommand");
            var doc = app.ActiveUIDocument.Document;
            
            // Your code here
            
            message = "Success!";
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            Console.WriteLine($"[Error] {ex.Message}");
            return Result.Failed;
        }
    }

    public Dictionary<string, object> GetCustomData() => _customData;
}
```

### Then Just Ask

```
Build MyPlugin and test MyCommand
```

I'll handle the rest!

## Troubleshooting

### "Revit is not ready"
- Start Revit 2025
- Open or create a document

### "Timeout waiting for result"
- Check Revit isn't frozen
- Increase timeout: "Execute with 120 second timeout"

### "Command failed"
- Check the logs - they show exactly what went wrong
- I can read the error and fix the code

## That's It!

You now have:
- ✅ MCP server configured
- ✅ Sample plugin built and tested
- ✅ AI can execute commands in Revit
- ✅ Hot-reload working
- ✅ Full development loop automated

Just ask me to build, test, or modify your Revit plugins!
