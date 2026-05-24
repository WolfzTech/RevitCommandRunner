# Installation Guide - RevitCommandRunner

## Prerequisites

- Windows 10/11
- Autodesk Revit 2021-2027 (any version)
- .NET Framework 4.8 (for Revit 2021-2022)
- .NET Framework 4.8 (for Revit 2021-2024)
- .NET 8.0 Runtime (for Revit 2025-2026)
- .NET 10.0 Runtime (for Revit 2027)
- PowerShell 5.1 or later

## Quick Installation

### Option 1: Installer.exe (Recommended)

1. **Download** the latest release: `RevitCommandRunner-v1.0.0.zip`

2. **Extract** to a folder (e.g., `C:\RevitCommandRunner`)

3. **Run installer:**
   ```powershell
   .\Installer.exe
   ```

4. **Start Revit** - The add-in will load automatically

### Option 2: PowerShell Installer

Run from the extracted release folder:
   ```powershell
   cd C:\RevitCommandRunner
   .\Install.ps1
   ```

### Option 3: Manual Installation

1. **Locate your Revit Addins folder:**
   ```
   %APPDATA%\Autodesk\Revit\Addins\[YEAR]\
   ```
   Example: `C:\Users\YourName\AppData\Roaming\Autodesk\Revit\Addins\2025\`

2. **Copy files:**
   - From: `RevitCommandRunner.bundle\Contents\[YEAR]\RevitCommandRunner.dll`
   - To: `%APPDATA%\Autodesk\Revit\Addins\[YEAR]\`
   
   - From: `RevitCommandRunner.addin`
   - To: `%APPDATA%\Autodesk\Revit\Addins\[YEAR]\`

3. **Repeat for each Revit version** you have installed

4. **Start Revit**

## Verification

### Check Add-in Loaded

1. Start Revit
2. No error dialogs should appear
3. Check: `%LOCALAPPDATA%\RevitCommandRunner\` folder should be created
4. File `command-queue.json` should exist

### Test with Sample Plugin

1. **Build sample plugin:**
   ```powershell
   cd samples\SamplePlugin
   dotnet build
   ```

2. **Test via PowerShell:**
   ```powershell
   Import-Module ..\..\tools\RevitCommandRunner.psm1
   
   $result = Invoke-RevitCommand `
       -DllPath ".\bin\Debug\net8.0-windows\SamplePlugin.dll" `
       -CommandClassName "SamplePlugin.HelloRevitCommand"
   
   Write-Host $result.message
   ```

3. **Expected output:**
   ```
   Hello from Revit! Document: Project1, Elements: 6078
   ```

## Configuration

### Default Configuration

Location: `%LOCALAPPDATA%\RevitCommandRunner\config.json`

```json
{
  "QueueFilePath": "%LOCALAPPDATA%\\RevitCommandRunner\\command-queue.json",
  "ResultsDirectory": "%LOCALAPPDATA%\\RevitCommandRunner\\results",
  "PollingIntervalMs": 500,
  "DeleteQueueFileAfterRead": true,
  "CaptureConsoleLogs": true,
  "MaxResultHistoryCount": 100
}
```

### Customization

Edit `config.json` to change:
- **PollingIntervalMs**: How often to check for commands (default: 500ms)
- **ResultsDirectory**: Where to store command results
- **CaptureConsoleLogs**: Enable/disable console output capture

## MCP Server Setup (for AI Integration)

The installer only installs the Revit add-in. To let an AI agent call Revit, configure the MCP server after installation.

### For OpenCode

1. **Build MCP server:**
   ```powershell
   cd mcp-server
   npm install
   npm run build
   ```

2. **Configure OpenCode:**
   Edit: `%USERPROFILE%\.config\opencode\opencode.jsonc`
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
   
   **Note**: The MCP server is installed automatically with the bundle. No separate download needed!

3. **Restart OpenCode**

### For Claude Desktop

Edit: `%APPDATA%\Claude\claude_desktop_config.json`
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

**Note**: The MCP server is installed automatically with the bundle. No separate download needed!

## Uninstallation

### Automated

```powershell
cd C:\RevitCommandRunner
.\Install.ps1 -Uninstall
```

### Manual

1. Delete files from each Revit Addins folder:
   - `RevitCommandRunner.addin`
   - `RevitCommandRunner.dll`
   - `RevitCommandRunner.pdb`
   - `Newtonsoft.Json.dll`

2. Delete configuration folder:
   ```
   %LOCALAPPDATA%\RevitCommandRunner\
   ```

## Troubleshooting

### Add-in Not Loading

**Check Revit version:**
```powershell
# List installed Revit versions
Get-ChildItem "$env:APPDATA\Autodesk\Revit\Addins"
```

**Check .addin file:**
- Open `RevitCommandRunner.addin` in text editor
- Verify `<Assembly>` path is correct
- Verify `<AddInId>` is a valid GUID

**Check DLL exists:**
```powershell
Test-Path "$env:APPDATA\Autodesk\Revit\Addins\2025\RevitCommandRunner.dll"
```

### "Assembly could not be loaded"

**For Revit 2021-2022:**
- Install .NET Framework 4.8
- Download: https://dotnet.microsoft.com/download/dotnet-framework/net48

**For Revit 2025-2026:**
- Install .NET 8.0 Runtime
- Download: https://dotnet.microsoft.com/download/dotnet/8.0

**For Revit 2027:**
- Install .NET 10.0 Runtime
- Download: https://dotnet.microsoft.com/download/dotnet/10.0

### "Command timeout"

Increase timeout in command execution:
```powershell
$result = Invoke-RevitCommand `
    -DllPath "..." `
    -CommandClassName "..." `
    -TimeoutSeconds 120
```

### "Queue file not found"

Check configuration:
```powershell
Get-Content "$env:LOCALAPPDATA\RevitCommandRunner\config.json"
```

Verify queue directory exists:
```powershell
Test-Path "$env:LOCALAPPDATA\RevitCommandRunner"
```

## Multi-Version Installation

If you have multiple Revit versions installed:

1. **Automated installer** handles all versions automatically
2. **Manual installation** - repeat for each version:
   - Revit 2021: Copy to `Addins\2021\`
   - Revit 2022: Copy to `Addins\2022\`
   - Revit 2023: Copy to `Addins\2023\`
   - etc.

Each version uses the **same configuration** at:
```
%LOCALAPPDATA%\RevitCommandRunner\config.json
```

## Upgrading

### From Previous Version

1. **Uninstall old version:**
   ```powershell
   .\Install.ps1 -Uninstall
   ```

2. **Install new version:**
   ```powershell
   .\Install.ps1
   ```

3. **Configuration is preserved** (not deleted during uninstall)

### Keeping Configuration

Your configuration at `%LOCALAPPDATA%\RevitCommandRunner\config.json` is **not deleted** during uninstall. To reset:

```powershell
Remove-Item "$env:LOCALAPPDATA\RevitCommandRunner" -Recurse -Force
```

## Support

- **GitHub Issues**: https://github.com/yourusername/RevitCommandRunner/issues
- **Documentation**: See README.md and USAGE_GUIDE.md
- **Sample Code**: See samples\SamplePlugin\

## Next Steps

After installation:

1. **Read USAGE_GUIDE.md** - Quick reference for AI usage
2. **Read HOT_RELOAD_EXPLAINED.md** - Understand hot-reload mechanism
3. **Try sample plugin** - Build and test HelloRevitCommand
4. **Create your own plugin** - Use sample as template
5. **Integrate with AI** - Configure MCP server for AI-driven development

## License

MIT License - See LICENSE file for details
