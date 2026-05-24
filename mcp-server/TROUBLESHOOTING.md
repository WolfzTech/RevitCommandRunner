# Troubleshooting MCP Server

## Common Issues and Solutions

### 1. "Unexpected server error" in OpenCode/Claude Desktop

**Possible causes:**
- Path format issues (backslashes vs forward slashes)
- Node.js not in PATH
- MCP server not built
- Permissions issues

**Solutions:**

#### A. Verify Node.js is accessible
```bash
node --version
```
Should output v18.0.0 or higher.

#### B. Verify MCP server is built
```bash
cd D:\RevitCommandRunner\mcp-server
npm run build
```

#### C. Test the server manually
```bash
node D:\RevitCommandRunner\mcp-server\dist\index.js
```
Should output: "RevitCommandRunner MCP server running on stdio"

#### D. Try different path formats

**For OpenCode:**

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

**For Claude Desktop:**

**Option 1: Escaped backslashes (Windows)**
```json
{
  "mcpServers": {
    "revit-command-runner": {
      "command": "node",
      "args": ["D:\\RevitCommandRunner\\mcp-server\\dist\\index.js"]
    }
  }
}
```

**Option 2: Forward slashes**
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

**Option 3: Use full node path**
```json
{
  "mcpServers": {
    "revit-command-runner": {
      "command": "C:\\Program Files\\nodejs\\node.exe",
      "args": ["D:\\RevitCommandRunner\\mcp-server\\dist\\index.js"]
    }
  }
}
```

#### E. Check OpenCode logs

**Windows**: Look for logs in:
- `%APPDATA%\opencode\logs\`
- `%USERPROFILE%\.config\opencode\logs\`

Look for error messages related to MCP server startup.

### 2. Server starts but tools don't work

**Check Revit is running:**
```bash
# In PowerShell
Test-Path "$env:LOCALAPPDATA\RevitCommandRunner"
```

**Verify RevitCommandRunner add-in is loaded:**
- Start Revit
- Should see "Command runner started" dialog
- Check `%LOCALAPPDATA%\RevitCommandRunner\` directory exists

### 3. Commands timeout

**Increase timeout:**
When calling `execute_revit_command`, increase the `timeoutSeconds` parameter:
```
Execute my command with a 120 second timeout
```

**Check Revit is not frozen:**
- Revit must be running and responsive
- No modal dialogs blocking execution

### 4. Permission errors

**Run as Administrator:**
Some operations may require elevated permissions. Try running OpenCode/Claude Desktop as administrator.

**Check file permissions:**
```bash
# Verify you can write to the queue directory
New-Item -ItemType File -Path "$env:LOCALAPPDATA\RevitCommandRunner\test.txt" -Force
Remove-Item "$env:LOCALAPPDATA\RevitCommandRunner\test.txt"
```

## Testing the MCP Server

### Manual Test Script

Run this to verify the server works:

```bash
node D:\RevitCommandRunner\mcp-server\test-server.mjs
```

Expected output:
```
Starting MCP server...
Sending initialize request...
Server stderr: RevitCommandRunner MCP server running on stdio
Server response: {"result":{"protocolVersion":"2024-11-05",...}}
Closing server...
```

### Test with MCP Inspector

Install the MCP Inspector:
```bash
npm install -g @modelcontextprotocol/inspector
```

Run the inspector:
```bash
mcp-inspector node D:\RevitCommandRunner\mcp-server\dist\index.js
```

This opens a web UI where you can test the MCP tools interactively.

## Getting Help

If issues persist:

1. **Check server logs**: Look in OpenCode/Claude Desktop logs for MCP-related errors
2. **Verify paths**: Ensure all paths in config are absolute and correct
3. **Test manually**: Run the test script to verify the server works standalone
4. **Check Node version**: Ensure Node.js 18+ is installed
5. **Restart the client**: After config changes, fully restart OpenCode/Claude Desktop

## Debug Mode

Enable verbose logging by modifying the server startup:

```json
{
  "mcpServers": {
    "revit-command-runner": {
      "command": "node",
      "args": ["D:\\RevitCommandRunner\\mcp-server\\dist\\index.js"],
      "env": {
        "DEBUG": "*"
      }
    }
  }
}
```

This will output detailed debug information to help diagnose issues.
