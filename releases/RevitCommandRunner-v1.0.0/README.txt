# RevitCommandRunner v1.0.0

AI-driven command execution framework for Autodesk Revit with hot-reload support.

## Supported Revit Versions

- Revit 2021-2024 (.NET Framework 4.8)
- Revit 2025-2026 (.NET 8.0)
- Revit 2027 (.NET 10.0)

## Installation

### Quick Install

1. Run `Installer.exe`
2. Start Revit
3. The add-in will load automatically

PowerShell alternative: run `Install.ps1`.

### Manual Install

1. Copy the appropriate DLL from `RevitCommandRunner.bundle\Contents\[YEAR]\` to:
   `%APPDATA%\Autodesk\Revit\Addins\[YEAR]\`

2. Copy `RevitCommandRunner.addin` to the same folder

3. Start Revit

## Configuration

The add-in creates a configuration file at:
`%LOCALAPPDATA%\RevitCommandRunner\config.json`

Default settings:
- Command queue polling: 500ms
- Console log capture: Enabled
- Queue file: `%LOCALAPPDATA%\RevitCommandRunner\command-queue.json`

## Usage

### With MCP Server (OpenCode/Claude)

Configure the MCP server in your AI tool:

``json
{
  "mcpServers": {
    "revit-command-runner": {
      "command": "node",
      "args": ["D:/RevitCommandRunner/mcp-server/build/index.js"]
    }
  }
}
``

Then ask your AI assistant:
- "Execute HelloRevitCommand from the sample plugin"
- "Build and test my Revit plugin"
- "Create 5 walls in the active document"

### With PowerShell

``powershell
Import-Module D:\RevitCommandRunner\tools\RevitCommandRunner.psm1

$result = Invoke-RevitCommand `
    -DllPath "C:\MyPlugin\bin\Debug\MyPlugin.dll" `
    -CommandClassName "MyNamespace.MyCommand"

Write-Host $result.message
``

## Hot-Reload

RevitCommandRunner supports hot-reload for user plugins:

1. Run your command
2. Modify your code
3. Rebuild (Revit still running!)
4. Run again - changes applied immediately

No Revit restart needed!

## Sample Plugin

See `samples\SamplePlugin\` for example commands:
- **HelloRevitCommand**: Read document info
- **CreateWallsCommand**: Create walls with transactions

## Documentation

- **README.md**: Overview and features
- **USAGE_GUIDE.md**: Quick reference for AI usage
- **HOT_RELOAD_EXPLAINED.md**: Technical details on hot-reload

## Support

- GitHub: https://github.com/yourusername/RevitCommandRunner
- Issues: https://github.com/yourusername/RevitCommandRunner/issues

## License

MIT License - See LICENSE file for details

## Version History

### v1.0.0
- Multi-version support (Revit 2021-2027)
- Hot-reload via collectible AssemblyLoadContext on Revit 2025+ and Assembly.Load(bytes) fallback on older Revit
- MCP server integration
- Sample plugins included
