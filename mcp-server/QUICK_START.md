# Quick Setup Guide - RevitCommandRunner MCP Server

## Prerequisites
- ✅ Revit 2025 installed
- ✅ RevitCommandRunner add-in running in Revit
- ✅ Node.js 18+ installed

## Setup Steps

### 1. Build MCP Server
```bash
cd D:\RevitCommandRunner\mcp-server
npm install
npm run build
```

### 2. Configure Your AI Agent

#### OpenCode
Edit: `%USERPROFILE%\.config\opencode\opencode.jsonc`
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

#### Claude Desktop
Edit: `%APPDATA%\Claude\claude_desktop_config.json`
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

### 3. Restart Your AI Agent

Close and reopen OpenCode or Claude Desktop.

### 4. Test It

Ask your AI agent:
```
Check if Revit is ready
```

Expected response: "Revit is ready to accept commands"

## Common Issues

### "Unexpected server error"
- ✅ Use forward slashes (`/`) in paths, not backslashes (`\`)
- ✅ Verify Node.js is in PATH: `node --version`
- ✅ Rebuild the server: `npm run build`
- ✅ See [TROUBLESHOOTING.md](TROUBLESHOOTING.md) for more solutions

### "Timeout waiting for result"
- ✅ Ensure Revit is running
- ✅ Check RevitCommandRunner add-in is loaded (see startup dialog)
- ✅ Verify directory exists: `%LOCALAPPDATA%\RevitCommandRunner`

### Tools not appearing
- ✅ Restart your AI agent completely
- ✅ Check config file syntax (valid JSON)
- ✅ Verify file path is correct

## Usage Examples

Once configured, you can ask your AI agent:

```
Execute my test command:
- DLL: D:\MyPlugin\bin\Debug\MyPlugin.dll
- Class: MyPlugin.TestCommand
```

```
Build my plugin and test it in Revit until all tests pass
```

```
Show me the last 5 command executions
```

## Available MCP Tools

1. **execute_revit_command** - Execute a command and get results
2. **get_command_result** - Retrieve results by ID
3. **check_revit_status** - Verify Revit is ready
4. **list_recent_results** - View command history

## Need Help?

- 📖 [Full Documentation](README.md)
- 🔧 [Troubleshooting Guide](TROUBLESHOOTING.md)
- 🧪 [Test the server](test-server.mjs): `node test-server.mjs`
