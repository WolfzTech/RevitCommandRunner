# RevitCommandRunner - AI-Driven Command Execution for Revit

## Project Overview

A standalone framework that enables AI agents to execute Revit commands programmatically with hot-reload support, eliminating the need for Revit restarts during development.

**Key Features:**
- Execute any `IExternalCommand` from any DLL
- Hot-reload: rebuild and re-run without restarting Revit
- File-based communication (works with any AI agent)
- Structured JSON results for AI consumption
- Full transaction context support
- Inspired by ricaun.RevitTest architecture

## Use Cases

1. **AI-driven development loop:**
   - AI modifies code → rebuild → execute → analyze results → repeat

2. **Automated regression testing:**
   - Run verification commands on multiple test files
   - AI fixes failures automatically

3. **Rapid prototyping:**
   - Test command changes instantly without Revit restart

## Architecture

```
AI Agent (Claude Code, Cursor, etc.)
    ↓ writes command-queue.json
    ↓
Revit Process
    ├─ CommandQueueMonitor (IExternalApplication)
    │   └─ FileSystemWatcher on command-queue.json
    │   └─ Triggers ExternalEvent
    │
    ├─ CommandExecutor (IExternalEventHandler)
    │   ├─ Hot-reload: Copy DLL to temp
    │   ├─ Load assembly via reflection
    │   ├─ Create command instance
    │   ├─ Execute with ExternalCommandData
    │   └─ Capture results
    │
    └─ ResultWriter
        └─ Writes results-{id}.json
    ↓
AI Agent reads results-{id}.json
```

## File Formats

### Command Queue: `command-queue.json`
```json
{
  "id": "run-001",
  "timestamp": "2026-05-14T14:30:00Z",
  "dll": "D:\\MyAddin\\bin\\Debug\\MyAddin.dll",
  "command": "MyNamespace.MyCommand",
  "args": ["arg1", "arg2"],
  "metadata": {
    "description": "Test verification on case_01",
    "iteration": 1
  }
}
```

### Results: `results-{id}.json`
```json
{
  "id": "run-001",
  "success": true,
  "result": "Succeeded",
  "message": "",
  "executionTimeMs": 1234,
  "timestamp": "2026-05-14T14:30:01Z",
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

### Exception format (when error occurs):
```json
{
  "exception": {
    "type": "System.InvalidOperationException",
    "message": "Transaction already started",
    "stackTrace": "at ...",
    "innerException": null
  }
}
```

## Implementation Plan

### Phase 1: Core Framework (Priority 1)

#### 1.1 Project Setup
- [x] Create standalone solution: `RevitCommandRunner.sln`
- [ ] Create library project: `RevitCommandRunner` (class library)
- [ ] Create sample add-in: `RevitCommandRunner.Sample` (for testing)
- [ ] Add NuGet packages:
  - Nice3point.Revit.Api (2025.x)
  - Newtonsoft.Json (for JSON serialization)

#### 1.2 Core Classes

**CommandQueueMonitor.cs** - IExternalApplication
```csharp
public class CommandQueueMonitor : IExternalApplication
{
    - FileSystemWatcher _watcher
    - ExternalEvent _externalEvent
    - CommandExecutor _executor
    
    + Result OnStartup(UIControlledApplication app)
    + Result OnShutdown(UIControlledApplication app)
    - void OnCommandFileChanged(object sender, FileSystemEventArgs e)
}
```

**CommandExecutor.cs** - IExternalEventHandler
```csharp
public class CommandExecutor : IExternalEventHandler
{
    + CommandRequest CurrentRequest { get; set; }
    
    + void Execute(UIApplication uiApp)
    - Assembly LoadAssemblyFromTemp(string dllPath)
    - IExternalCommand CreateCommandInstance(Assembly asm, string className)
    - ExternalCommandData CreateCommandData(UIApplication uiApp)
    - CommandResult ExecuteCommand(IExternalCommand cmd, ExternalCommandData data)
    + string GetName()
}
```

**CommandRequest.cs** - Data model
```csharp
public class CommandRequest
{
    + string Id { get; set; }
    + DateTime Timestamp { get; set; }
    + string DllPath { get; set; }
    + string CommandClassName { get; set; }
    + List<string> Args { get; set; }
    + Dictionary<string, object> Metadata { get; set; }
    
    + static CommandRequest FromJson(string json)
    + string ToJson()
}
```

**CommandResult.cs** - Result model
```csharp
public class CommandResult
{
    + string Id { get; set; }
    + bool Success { get; set; }
    + string Result { get; set; } // "Succeeded", "Failed", "Cancelled"
    + string Message { get; set; }
    + long ExecutionTimeMs { get; set; }
    + DateTime Timestamp { get; set; }
    + List<string> Logs { get; set; }
    + ExceptionInfo Exception { get; set; }
    + Dictionary<string, object> CustomData { get; set; }
    
    + string ToJson()
    + void WriteToFile(string path)
}
```

**ExceptionInfo.cs** - Exception capture
```csharp
public class ExceptionInfo
{
    + string Type { get; set; }
    + string Message { get; set; }
    + string StackTrace { get; set; }
    + ExceptionInfo InnerException { get; set; }
    
    + static ExceptionInfo FromException(Exception ex)
}
```

**LogCapture.cs** - Capture logs during execution
```csharp
public class LogCapture : IDisposable
{
    - StringWriter _stringWriter
    - TextWriter _originalOut
    
    + LogCapture()
    + List<string> GetLogs()
    + void Dispose()
}
```

#### 1.3 Configuration

**RevitCommandRunnerConfig.cs**
```csharp
public class RevitCommandRunnerConfig
{
    + string QueueFilePath { get; set; }
    + string ResultsDirectory { get; set; }
    + int MaxExecutionTimeSeconds { get; set; }
    + bool CaptureConsoleLogs { get; set; }
    + bool DeleteQueueFileAfterRead { get; set; }
    
    + static RevitCommandRunnerConfig Default { get; }
    + static RevitCommandRunnerConfig FromFile(string path)
}
```

Default paths:
- Queue: `%LOCALAPPDATA%\RevitCommandRunner\command-queue.json`
- Results: `%LOCALAPPDATA%\RevitCommandRunner\results\`
- Config: `%LOCALAPPDATA%\RevitCommandRunner\config.json`

### Phase 2: AI Helper Tools (Priority 2)

#### 2.1 PowerShell Module

**RevitCommandRunner.psm1**
```powershell
function Invoke-RevitCommand {
    param(
        [string]$DllPath,
        [string]$Command,
        [string[]]$Args,
        [int]$TimeoutSeconds = 60
    )
    
    # Generate unique ID
    # Write command-queue.json
    # Wait for results file
    # Parse and return results
    # Return PSObject with properties
}

function Get-RevitCommandResult {
    param([string]$Id)
    # Read results-{id}.json
    # Return PSObject
}

function Clear-RevitCommandQueue {
    # Delete queue file
}

Export-ModuleMember -Function Invoke-RevitCommand, Get-RevitCommandResult, Clear-RevitCommandQueue
```

#### 2.2 Python Helper (optional)

**revit_command_runner.py**
```python
class RevitCommandRunner:
    def execute_command(dll_path, command, args, timeout=60):
        # Write queue file
        # Wait for results
        # Return dict
    
    def get_result(id):
        # Read results file
        # Return dict
```

### Phase 3: Enhanced Features (Priority 3)

#### 3.1 Advanced Execution
- [ ] Timeout handling (kill hung commands)
- [ ] Parallel execution queue (multiple commands)
- [ ] Command history/replay
- [ ] Retry logic with exponential backoff

#### 3.2 Integration Helpers
- [ ] Custom log providers (integrate with existing logging)
- [ ] Result callbacks (webhook/HTTP POST)
- [ ] MCP server wrapper (optional)

#### 3.3 Developer Experience
- [ ] Visual Studio extension (right-click → Run in Revit)
- [ ] Command palette in Revit (UI for manual testing)
- [ ] Real-time log streaming (WebSocket)

## Project Structure

```
RevitCommandRunner/
├── RevitCommandRunner.sln
├── README.md
├── PLAN.md (this file)
├── LICENSE
│
├── src/
│   ├── RevitCommandRunner/
│   │   ├── RevitCommandRunner.csproj
│   │   ├── Core/
│   │   │   ├── CommandQueueMonitor.cs
│   │   │   ├── CommandExecutor.cs
│   │   │   └── LogCapture.cs
│   │   ├── Models/
│   │   │   ├── CommandRequest.cs
│   │   │   ├── CommandResult.cs
│   │   │   ├── ExceptionInfo.cs
│   │   │   └── RevitCommandRunnerConfig.cs
│   │   └── Utils/
│   │       ├── AssemblyLoader.cs
│   │       └── FileSystemHelper.cs
│   │
│   └── RevitCommandRunner.Sample/
│       ├── RevitCommandRunner.Sample.csproj
│       ├── SampleCommand.cs
│       └── RevitCommandRunner.Sample.addin
│
├── tools/
│   ├── RevitCommandRunner.psm1
│   └── revit_command_runner.py
│
├── tests/
│   └── RevitCommandRunner.Tests/
│       └── (unit tests)
│
└── docs/
    ├── getting-started.md
    ├── api-reference.md
    └── examples.md
```

## Installation & Usage

### For Add-in Developers

1. **Add NuGet package** (future):
   ```
   Install-Package RevitCommandRunner
   ```

2. **Register in .addin file**:
   ```xml
   <AddIn Type="Application">
     <Assembly>path\to\RevitCommandRunner.dll</Assembly>
     <FullClassName>RevitCommandRunner.CommandQueueMonitor</FullClassName>
     <ClientId>...</ClientId>
   </AddIn>
   ```

3. **Configure** (optional):
   Create `%LOCALAPPDATA%\RevitCommandRunner\config.json`

### For AI Agents

**PowerShell:**
```powershell
Import-Module .\RevitCommandRunner.psm1

$result = Invoke-RevitCommand `
    -DllPath "D:\MyAddin\bin\Debug\MyAddin.dll" `
    -Command "MyNamespace.VerifyCommand" `
    -Args @("C:\test.ifc")

if ($result.Success) {
    Write-Host "PASS: $($result.CustomData.passCount)"
} else {
    Write-Host "FAIL: $($result.Exception.Message)"
}
```

**Direct file manipulation:**
```powershell
# Write command
$cmd = @{
    id = "run-001"
    timestamp = (Get-Date).ToString("o")
    dll = "D:\MyAddin\bin\Debug\MyAddin.dll"
    command = "MyNamespace.VerifyCommand"
    args = @("C:\test.ifc")
} | ConvertTo-Json

Set-Content -Path "$env:LOCALAPPDATA\RevitCommandRunner\command-queue.json" -Value $cmd

# Wait and read result
Start-Sleep -Seconds 2
$result = Get-Content "$env:LOCALAPPDATA\RevitCommandRunner\results\results-run-001.json" | ConvertFrom-Json
```

## Testing Strategy

### Unit Tests
- CommandRequest serialization/deserialization
- CommandResult serialization/deserialization
- ExceptionInfo capture
- Config loading

### Integration Tests (require Revit)
- Load sample command from DLL
- Execute command with mock data
- Capture results correctly
- Hot-reload works (execute same command twice with different DLL versions)

### End-to-End Tests
- Full workflow: write queue → execute → read results
- Multiple commands in sequence
- Error handling (missing DLL, invalid class name, etc.)

## Current Status

- [x] Plan created
- [ ] Project structure created
- [ ] Core classes implemented
- [ ] PowerShell module created
- [ ] Sample add-in created
- [ ] Documentation written
- [ ] Tested with real Revit add-in

## Next Steps

1. Create project structure
2. Implement core classes (Phase 1.2)
3. Create sample command for testing
4. Test hot-reload mechanism
5. Build PowerShell helper
6. Document usage
7. Test with Dial_ConvertIFCLuminaire project

## Notes

- **Revit version**: Start with 2025, make version-agnostic later
- **JSON library**: Newtonsoft.Json (most compatible)
- **Hot-reload**: Copy to `%TEMP%\RevitCommandRunner\{timestamp}\{dll-name}.dll`
- **Thread safety**: ExternalEvent ensures main thread execution
- **Transaction context**: Commands create their own transactions (like normal)

## Future Enhancements

- Support for `IExternalApplication` execution (not just commands)
- Support for Dynamo scripts
- Support for pyRevit scripts
- Web dashboard for monitoring executions
- Integration with CI/CD pipelines
- Cloud-based command queue (Azure/AWS)

## References

- [ricaun.RevitTest](https://github.com/ricaun-io/ricaun.RevitTest) - Inspiration for hot-reload
- [Revit API Docs](https://www.revitapidocs.com/) - IExternalCommand, ExternalEvent
- [Add-in Manager](https://github.com/chuongmep/RevitAddInManager) - Hot-reload reference

---

**Last Updated**: 2026-05-14
**Status**: Planning → Implementation
**Next Session**: Start with Phase 1.1 - Project Setup
