# Agent Notes

## Scope

- This repo is intentionally small: keep only the Revit add-in (`src/RevitCommandRunner`), MCP server (`mcp-server`), WPF installer (`installer/RevitCommandRunnerInstaller`), and release scripts.
- Do not reintroduce old release/docs/sample/tool folders unless explicitly requested.

## Build Commands

- Preferred full release build from repo root: `Rebuild-Installer.bat 1.0.2`.
- The batch requires `pwsh`, `dotnet`, `node`, and `npm`; it intentionally keeps the terminal open with `cmd /k`/`pause`.
- The batch runs, in order: Revit multi-version build, `npm install`, `npm run build`, embedded installer packaging.
- Use `pwsh`, not Windows PowerShell, for `.ps1` scripts. `Build-AllVersions.ps1` uses syntax that failed under Windows PowerShell.
- Focused checks: `npm run build --prefix mcp-server` and `dotnet publish installer\RevitCommandRunnerInstaller -c Release -r win-x64 --self-contained true`.

## Release Output

- Distribute only `releases\RevitCommandRunner-v<version>-Installer.exe`.
- Release folders/zips, `build/`, `mcp-server/dist/`, `mcp-server/node_modules/`, and installer `bin/obj` are generated and ignored.
- If `Create-Embedded-Installer.ps1` cannot overwrite the release EXE, an old installer process is probably still running and locking it.

## Revit Add-In Build

- `src/RevitCommandRunner/Build-AllVersions.ps1` builds Revit 2021-2027 and temporarily edits `RevitCommandRunner.csproj` to inject per-version Revit API packages; it restores the original file in `finally`.
- The installed bundle path is `%APPDATA%\Autodesk\ApplicationPlugins\RevitCommandRunner.bundle`.
- Revit locks installed DLLs while running. Close Revit before reinstalling or replacing the bundle.

## MCP Runtime

- MCP server source is `mcp-server/src/index.ts`; compiled files are copied from `mcp-server/dist` into the installer bundle.
- Runtime IPC is file-based under `%LOCALAPPDATA%\RevitCommandRunner`: `command-queue.json` and `results\results-{id}.json`.
- The Revit entrypoint is `CommandQueueMonitor : IExternalApplication`; it watches the queue file and raises an `ExternalEvent` to execute commands safely in Revit.

## Installer MCP Config Gotchas

- The WPF installer updates existing `revit-command-runner` entries instead of duplicating them and preserves unrelated MCP servers.
- Claude Code global config goes in top-level `%USERPROFILE%\.claude.json -> mcpServers`; do not write under `projects` for this global Revit tool.
- Claude Desktop config goes in `%APPDATA%\Claude\claude_desktop_config.json -> mcpServers` and uses `command` plus `args`, without `type`.
- Claude Code and OpenCode should receive the fully expanded installed MCP path (`C:/Users/.../AppData/Roaming/.../index.js`), not `%APPDATA%`.
- OpenCode schema is `mcp.revit-command-runner = { "type": "local", "command": ["node", "<path>"], "enabled": true }`; it does not use `args`.
- Antigravity schema needs the `cmd /c` wrapper so `%APPDATA%` expands: `command: "cmd"`, `args: ["/c", "node", "%APPDATA%\\Autodesk\\ApplicationPlugins\\RevitCommandRunner.bundle\\mcp-server\\index.js"]`.
- Cursor config goes in `%USERPROFILE%\.cursor\mcp.json` and uses the same format as Claude Desktop (`command` plus `args`, without `type`).
- Config parsing allows comments/trailing commas, but structurally malformed JSON is skipped per client so installation can still complete.
