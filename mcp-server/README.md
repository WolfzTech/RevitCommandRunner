# RevitCommandRunner MCP Server

Model Context Protocol (MCP) server for RevitCommandRunner. Enables AI agents like Claude Desktop to execute Revit commands directly through standardized MCP tools.

## What is this?

This MCP server wraps RevitCommandRunner, allowing AI agents to:
- Execute Revit commands from any DLL
- Check command execution results
- Monitor Revit status
- View command history

All through the standardized Model Context Protocol interface.

## Installation

### Prerequisites

1. **RevitCommandRunner installed** - The base framework must be running in Revit
2. **Node.js 18+** - Required for the MCP server
3. **Revit 2025** - With RevitCommandRunner add-in loaded

### Install MCP Server

```bash
cd D:\RevitCommandRunner\mcp-server
npm install
npm run build
```

## Configuration

### For OpenCode

Add to your OpenCode config file:

**Windows**: `%USERPROFILE%\.config\opencode\opencode.jsonc`

```json
{
  "mcp": {
    "revit-command-runner": {
      "type": "local",
      "command": [
        "node",
        "D:/RevitCommandRunner/mcp-server/dist/index.js"
      ],
      "enabled": true
    }
  }
}
```

**Note**: OpenCode uses `"mcp"` (not `"mcpServers"`) and `"command"` is an array. Use forward slashes (`/`) in paths. After saving, restart OpenCode to load the MCP server.

**Troubleshooting**: If you get "Unexpected server error", see [TROUBLESHOOTING.md](TROUBLESHOOTING.md) for solutions.

### For Claude Desktop

Add to your Claude Desktop config file:

**Windows**: `%APPDATA%\Claude\claude_desktop_config.json`

```json
{
  "mcpServers": {
    "revit-command-runner": {
      "command": "node",
      "args": ["D:/RevitCommandRunner/mcp-server/dist/index.js"]
    }
  }
}
```

### For Other MCP Clients

Use the standard MCP stdio transport:

```bash
node D:\RevitCommandRunner\mcp-server\dist\index.js
```

## Available Tools

### 1. execute_revit_command

Execute a Revit command from a DLL and wait for results.

**Parameters:**
- `dllPath` (required): Absolute path to the DLL containing the command
- `commandClassName` (required): Fully qualified class name (e.g., `MyNamespace.MyCommand`)
- `args` (optional): Array of string arguments to pass to the command
- `timeoutSeconds` (optional): Maximum wait time in seconds (default: 60)

**Returns:**
```json
{
  "id": "mcp-1234567890-abc123",
  "success": true,
  "result": "Succeeded",
  "message": "10 passed, 0 failed",
  "executionTimeMs": 1234,
  "timestamp": "2026-05-24T10:30:00Z",
  "logs": [
    "[Info] Starting command execution",
    "[OK] Processed 10 items"
  ],
  "exception": null,
  "customData": {
    "passCount": 10,
    "failCount": 0
  }
}
```

**Example usage in AI agents:**
```
Execute the verification command from my plugin:
- DLL: D:\MyPlugin\bin\Debug\MyPlugin.dll
- Class: MyPlugin.VerifyCommand
```

### 2. get_command_result

Retrieve results from a previously executed command by ID.

**Parameters:**
- `id` (required): Command ID returned from `execute_revit_command`

**Returns:** Same format as `execute_revit_command`

**Example usage in AI agents:**
```
Get the result for command ID: mcp-1234567890-abc123
```

### 3. check_revit_status

Check if Revit is running and ready to accept commands.

**Parameters:** None

**Returns:**
```json
{
  "ready": true,
  "queueDirectory": "C:\\Users\\YourName\\AppData\\Local\\RevitCommandRunner",
  "message": "Revit is ready to accept commands"
}
```

**Example usage in AI agents:**
```
Is Revit ready to execute commands?
```

### 4. list_recent_results

List recent command execution results.

**Parameters:**
- `limit` (optional): Maximum number of results to return (default: 10)

**Returns:** Array of command results

**Example usage in AI agents:**
```
Show me the last 5 command executions
```

## Usage Examples

### Basic Command Execution

```
User: Execute my test command in Revit
AI Agent: [Uses execute_revit_command tool]
          DLL: D:\MyPlugin\bin\Debug\MyPlugin.dll
          Class: MyPlugin.TestCommand
          
          Result: Success! Command executed in 1.2 seconds.
          Logs show 10 items were processed.
```

### AI Development Loop

```
User: Build and test my plugin until all tests pass

AI Agent: 1. Building plugin...
          [Runs: dotnet build]
          
          2. Executing in Revit...
          [Uses execute_revit_command tool]
        
        Result: 2 tests failed
        - Test_WallCreation: NullReferenceException at line 45
        - Test_DoorPlacement: Transaction not started
        
        3. Fixing issues...
        [Edits code files]
        
        4. Rebuilding and retesting...
        [Repeats until all tests pass]
        
        ✓ All tests passed!
```

### Checking Status

```
User: Is Revit ready?
AI Agent: [Uses check_revit_status tool]
          
          Yes, Revit is running and ready to accept commands.
          Queue directory: C:\Users\...\RevitCommandRunner
```

## How It Works

```
┌─────────────────┐
│   AI Agent      │
│ (OpenCode,      │
│  Claude, etc.)  │
└────────┬────────┘
         │ MCP Protocol
         │ (stdio)
┌────────▼────────┐
│   MCP Server    │
│   (Node.js)     │
└────────┬────────┘
         │ File System
         │ (JSON files)
┌────────▼────────┐
│ RevitCommand    │
│    Runner       │
│ (Revit Add-in)  │
└────────┬────────┘
         │ ExternalEvent
┌────────▼────────┐
│     Revit       │
│   Process       │
└─────────────────┘
```

1. AI agent calls MCP tool (e.g., `execute_revit_command`)
2. MCP server writes `command-queue.json`
3. RevitCommandRunner monitors file and executes command
4. Results written to `results-{id}.json`
5. MCP server reads and returns results to AI agent

## File Locations

- **Queue file**: `%LOCALAPPDATA%\RevitCommandRunner\command-queue.json`
- **Results**: `%LOCALAPPDATA%\RevitCommandRunner\results\results-{id}.json`

## Troubleshooting

### "Timeout waiting for result"

- Ensure Revit is running
- Ensure RevitCommandRunner add-in is loaded (check Revit startup dialog)
- Check if command DLL path is correct
- Increase `timeoutSeconds` parameter

### "Revit is not ready"

- Start Revit 2025
- Verify RevitCommandRunner.addin is in `C:\ProgramData\Autodesk\Revit\Addins\2025\`
- Check Revit shows "Command runner started" dialog on startup

### "Cannot find module"

- Run `npm install` in the mcp-server directory
- Run `npm run build` to compile TypeScript

### Command executes but returns error

- Check the `exception` field in the result
- Review `logs` array for error messages
- Verify command implements `IExternalCommand` or `IExternalCommandWithUIApp`

## Development

### Build

```bash
npm run build
```

### Watch mode (auto-rebuild)

```bash
npm run watch
```

### Testing

```bash
# Start MCP server manually
node dist/index.js

# In another terminal, send MCP requests
# (Use MCP inspector or client)
```

## Architecture

- **TypeScript** - Type-safe implementation
- **@modelcontextprotocol/sdk** - Official MCP SDK
- **stdio transport** - Standard MCP communication
- **File-based IPC** - Communicates with RevitCommandRunner via JSON files

## Requirements

- Node.js 18+
- RevitCommandRunner installed and running in Revit
- Revit 2025
- Windows

## License

MIT

## See Also

- [RevitCommandRunner README](../README.md) - Main framework documentation
- [MCP Specification](https://modelcontextprotocol.io/) - Model Context Protocol docs
- [Claude Desktop MCP Guide](https://docs.anthropic.com/claude/docs/model-context-protocol) - Using MCP with Claude
