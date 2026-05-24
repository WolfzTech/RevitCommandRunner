# Assembly Unloading Strategy

RevitCommandRunner uses two hot-reload strategies depending on the Revit runtime.

## Revit 2021-2024

These versions run on .NET Framework 4.8.

Strategy: `Assembly.Load(byte[])`

This supports hot-reload because each execution loads a fresh assembly image from bytes, avoiding `Assembly.LoadFrom` cache behavior. However, .NET Framework cannot unload individual assemblies from the default AppDomain, so loaded plugin assemblies remain in memory until Revit exits.

## Revit 2025-2027

These versions run on modern .NET.

Strategy: collectible `AssemblyLoadContext`

The plugin DLL is loaded into a collectible context, executed, then the context is unloaded. This is better for long Revit sessions because old plugin versions can be garbage-collected instead of accumulating indefinitely.

## Why This Is A Good Idea

- Reduces memory growth during repeated AI build/test loops.
- Avoids stale assembly identities across hot-reload iterations.
- Keeps Revit 2025+ behavior closer to a true plugin sandbox.
- Still preserves compatibility with Revit 2021-2024 through the byte-load fallback.

## Important Caveats

Unloading only succeeds if no live references remain to types or objects from the loaded plugin assembly. A plugin can prevent unloading if it:

- Registers static events and does not unregister them.
- Starts background threads or timers.
- Stores plugin objects in Revit extensible storage, static caches, or external singletons.
- Returns objects from plugin-defined types that are held after execution.

Best practice for commands executed by RevitCommandRunner:

- Keep commands synchronous and short-lived.
- Return only primitive/JSON-friendly custom data: strings, numbers, booleans, arrays, dictionaries.
- Avoid static mutable state in command assemblies.
- Dispose timers, file watchers, and external resources before returning.

## Current Behavior

The command result logs include the active loading strategy:

```text
[Info] Assembly loading strategy: AssemblyLoadContext (collectible) - Supports true unloading
```

or on older Revit:

```text
[Info] Assembly loading strategy: Assembly.Load(bytes) - Hot-reload only, no unloading
```
