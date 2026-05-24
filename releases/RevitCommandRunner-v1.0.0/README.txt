# RevitCommandRunner v1.0.0 - Installer Package

AI-driven command execution framework for Autodesk Revit with hot-reload support.

## Installation

**Run Installer.exe** - That's it!

The installer will:
- Install the bundle to ApplicationPlugins
- Register in Windows Programs & Features
- Include MCP server with dependencies

## Supported Revit Versions

- Revit 2021-2024 (.NET Framework 4.8)
- Revit 2025-2026 (.NET 8.0)
- Revit 2027 (.NET 10.0)

## After Installation

### Configure AI Agent (Optional)

**For OpenCode:**
Edit `%USERPROFILE%\.config\opencode\opencode.jsonc`:
``json
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
``

**For Claude Desktop:**
Edit `%APPDATA%\Claude\claude_desktop_config.json`:
``json
{
  "mcpServers": {
    "revit-command-runner": {
      "command": "node",
      "args": ["%APPDATA%/Autodesk/ApplicationPlugins/RevitCommandRunner.bundle/mcp-server/index.js"]
    }
  }
}
``

### Start Revit

The add-in loads automatically. No dialogs, no UI.

## Uninstallation

**Option 1:** Windows Settings → Apps → RevitCommandRunner → Uninstall

**Option 2:** Run `Installer.exe --uninstall`

## Documentation

See the included documentation files for detailed information:
- README.md - Full documentation
- INSTALLATION.md - Installation guide
- USAGE_GUIDE.md - Usage examples
- HOT_RELOAD_EXPLAINED.md - How hot-reload works
- ASSEMBLY_UNLOADING.md - Technical details

## Sample Plugin

The `samples/SamplePlugin` directory contains example commands you can use as a starting point.

## Support

- GitHub: https://github.com/yourusername/RevitCommandRunner
- Issues: https://github.com/yourusername/RevitCommandRunner/issues

---

**Quick Start:** Run Installer.exe, configure your AI agent, start Revit!
