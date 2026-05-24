# Hot-Reload Mechanism Explained

## The Problem

.NET assemblies loaded via `Assembly.LoadFrom(path)` are cached by their **strong name** (assembly name + version + culture + public key token). Even if you copy the DLL to a different location, .NET returns the already-loaded assembly if it has the same identity.

This prevents hot-reload - you'd need to restart Revit every time you rebuild your plugin.

## The Solution

We changed the assembly loading mechanism from:

```csharp
// OLD: Returns cached assembly with same name
Assembly assembly = Assembly.LoadFrom(tempDllPath);
```

To:

```csharp
// NEW: Loads fresh copy every time
byte[] assemblyBytes = File.ReadAllBytes(tempDllPath);
Assembly assembly = Assembly.Load(assemblyBytes);

// Also load PDB for debugging
string tempPdbPath = Path.ChangeExtension(tempDllPath, ".pdb");
if (File.Exists(tempPdbPath))
{
    byte[] pdbBytes = File.ReadAllBytes(tempPdbPath);
    assembly = Assembly.Load(assemblyBytes, pdbBytes);
}
```

**Key difference**: `Assembly.Load(byte[])` loads from raw bytes and creates a **new assembly instance** each time, bypassing the assembly cache.

## How It Works

### 1. Copy to Temp with Timestamp
```csharp
string tempDllPath = AssemblyLoader.CopyToTempWithTimestamp(request.DllPath);
// Example: SamplePlugin_20260524102840123.dll
```

Each execution copies the DLL to a unique temp file with a timestamp.

### 2. Load from Bytes
```csharp
byte[] bytes = File.ReadAllBytes(tempDllPath);
Assembly assembly = Assembly.Load(bytes);
```

Loading from bytes creates a fresh assembly instance that doesn't conflict with previously loaded assemblies.

### 3. Execute Command
```csharp
Type commandType = assembly.GetTypes()
    .FirstOrDefault(t => t.FullName == request.CommandClassName);
var command = Activator.CreateInstance(commandType);
command.Execute(...);
```

The command is instantiated from the fresh assembly and executed.

## What Can Be Hot-Reloaded

### ✅ YES - User Plugins
- **SamplePlugin.dll**
- **Dial_ConvertIFCLuminaire.dll**
- **Any DLL you pass to `execute_revit_command`**

These are loaded via `Assembly.Load(bytes)` and can be rebuilt and re-executed without restarting Revit.

### ❌ NO - RevitCommandRunner Add-in
- **RevitCommandRunner.dll** (the core add-in)

This is loaded by Revit at startup and cannot be unloaded. Changes to the core framework require a Revit restart.

## Testing Hot-Reload

### Step 1: Restart Revit (One Time Only)
Close and restart Revit to:
1. Release the lock on RevitCommandRunner.dll
2. Allow rebuilding with the hot-reload fix
3. Load the new version with `Assembly.Load(bytes)`

### Step 2: Build RevitCommandRunner (One Time Only)
```bash
dotnet build D:\RevitCommandRunner\src\RevitCommandRunner\RevitCommandRunner.csproj
```

This must be done **while Revit is closed**.

### Step 3: Start Revit
Start Revit with the new RevitCommandRunner add-in loaded.

### Step 4: Test Hot-Reload Loop
Now you can test the hot-reload cycle:

```bash
# 1. Run original version
Execute SamplePlugin.HelloRevitCommand

# 2. Modify the code
Edit D:\RevitCommandRunner\samples\SamplePlugin\HelloRevitCommand.cs
# Change message to "VERSION 2.0"

# 3. Rebuild (Revit still running!)
dotnet build D:\RevitCommandRunner\samples\SamplePlugin\SamplePlugin.csproj

# 4. Run again WITHOUT restarting Revit
Execute SamplePlugin.HelloRevitCommand

# 5. See the new version!
# Output: "Hello from Revit! ... [HOT-RELOADED v2.0]"
```

## Benefits

### 🚀 Faster Development
- No Revit restart between code changes
- Rebuild and test in seconds
- Iterate quickly on plugin logic

### 🔄 True Hot-Reload
- Each execution loads the latest DLL
- No assembly caching issues
- Works like Add-In Manager

### 🧪 Perfect for AI Development
- AI can modify code
- Build automatically
- Test immediately
- Fix errors and retry
- All without human intervention to restart Revit

## Limitations

### Assembly Dependencies
If your plugin references other DLLs, those are also loaded from bytes. However, if they reference **Revit API assemblies** (RevitAPI.dll, RevitAPIUI.dll), those are resolved from Revit's installation directory.

### Static State
Static fields and singletons are **not reset** between hot-reloads. Each assembly instance has its own static state, but they all share the same Revit process.

### Memory Usage
Each hot-reload creates a new assembly instance in memory. The old instances are eventually garbage collected, but frequent reloads can increase memory usage temporarily.

### Cleanup
Temp DLLs accumulate in `%TEMP%\RevitCommandRunner\`. The `AssemblyLoader.CleanupOldTempFiles()` method can be called periodically to remove old files.

## Comparison with Add-In Manager

### Add-In Manager
- Loads assemblies via reflection
- Uses `Assembly.LoadFrom()` or similar
- Requires manual "Reload" button click
- May have assembly caching issues

### RevitCommandRunner
- Loads assemblies from bytes
- Uses `Assembly.Load(byte[])`
- Automatic reload on every execution
- No caching issues
- Works seamlessly with AI agents

## Technical Details

### Why Assembly.Load(byte[]) Works

When you call `Assembly.LoadFrom(path)`, .NET:
1. Checks if an assembly with the same identity is already loaded
2. If yes, returns the cached assembly
3. If no, loads from the file

When you call `Assembly.Load(byte[])`, .NET:
1. **Skips the cache check**
2. Creates a new assembly instance from the bytes
3. Assigns it a unique identity in the current AppDomain

This is why loading from bytes enables true hot-reload.

### Assembly Identity

Each assembly loaded from bytes gets a unique identity:
```
SamplePlugin, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null [Instance 1]
SamplePlugin, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null [Instance 2]
SamplePlugin, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null [Instance 3]
```

They have the same name but are different instances in memory.

### Type Compatibility

Types from different assembly instances are **not compatible**:
```csharp
// Instance 1
var obj1 = new SamplePlugin.HelloRevitCommand();

// Instance 2 (after hot-reload)
var obj2 = new SamplePlugin.HelloRevitCommand();

// These are DIFFERENT types!
obj1.GetType() != obj2.GetType()  // true
```

This is why we create a fresh instance for each execution.

## Future Improvements

### 1. Assembly Unloading (Requires .NET Core 3.0+)
```csharp
var alc = new AssemblyLoadContext("PluginContext", isCollectible: true);
Assembly assembly = alc.LoadFromStream(new MemoryStream(bytes));
// ... use assembly ...
alc.Unload();  // Unload when done
```

This would allow true unloading and reduce memory usage.

### 2. Dependency Resolution
Improve handling of plugin dependencies by implementing a custom `AssemblyLoadContext` with proper dependency resolution.

### 3. Automatic Cleanup
Periodically clean up old temp DLLs to prevent disk bloat.

### 4. Performance Monitoring
Track assembly load times and memory usage to optimize the hot-reload mechanism.

## Conclusion

The hot-reload mechanism using `Assembly.Load(byte[])` provides a seamless development experience for Revit plugins. It enables rapid iteration without Revit restarts, making it perfect for AI-driven development workflows.

**One-time setup**: Restart Revit once to load the updated RevitCommandRunner add-in.

**After that**: Unlimited hot-reloads of your plugins without any Revit restarts!
