using System;
using System.IO;
using System.Reflection;
#if NETCOREAPP
using System.Runtime.Loader;
#endif

namespace RevitCommandRunner.Utils
{
#if NETCOREAPP
    /// <summary>
    /// Collectible assembly load context for true hot-reload with unloading.
    /// Only available in .NET Core/.NET 5+ (Revit 2025+ uses .NET 8).
    /// </summary>
    internal sealed class CollectibleAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        public CollectibleAssemblyLoadContext(string pluginPath) : base(isCollectible: true)
        {
            _resolver = new AssemblyDependencyResolver(pluginPath);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            return assemblyPath == null ? null : LoadFromAssemblyPath(assemblyPath);
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            return libraryPath == null ? IntPtr.Zero : LoadUnmanagedDllFromPath(libraryPath);
        }
    }
#endif

    /// <summary>
    /// Assembly loader with version-specific strategies.
    /// Uses AssemblyLoadContext for .NET 8.0+ (Revit 2025+) and Assembly.Load(bytes) for older versions.
    /// </summary>
    public static class AssemblyLoaderV2
    {
        /// <summary>
        /// Load assembly with the best available strategy for the current runtime.
        /// Returns the assembly and an optional unload action.
        /// </summary>
        public static (Assembly Assembly, Action? Unload) LoadAssembly(string dllPath)
        {
#if NETCOREAPP
            return LoadWithContext(dllPath);
#else
            return LoadFromBytes(dllPath);
#endif
        }

#if NETCOREAPP
        /// <summary>
        /// Load assembly using collectible AssemblyLoadContext (.NET 8.0+).
        /// Supports true unloading after execution.
        /// </summary>
        private static (Assembly, Action?) LoadWithContext(string dllPath)
        {
            var context = new CollectibleAssemblyLoadContext(dllPath);
            var assembly = context.LoadFromAssemblyPath(dllPath);

            // Return unload action
            Action unload = () =>
            {
                context.Unload();
                
                // Force garbage collection to free memory
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            };

            return (assembly, unload);
        }
#endif

        /// <summary>
        /// Load assembly from bytes (fallback for .NET Framework 4.8).
        /// No unloading support, but enables hot-reload.
        /// </summary>
        private static (Assembly, Action?) LoadFromBytes(string dllPath)
        {
            byte[] assemblyBytes = File.ReadAllBytes(dllPath);
            
            // Try to load PDB for debugging
            string pdbPath = Path.ChangeExtension(dllPath, ".pdb");
            Assembly assembly;
            
            if (File.Exists(pdbPath))
            {
                byte[] pdbBytes = File.ReadAllBytes(pdbPath);
                assembly = Assembly.Load(assemblyBytes, pdbBytes);
            }
            else
            {
                assembly = Assembly.Load(assemblyBytes);
            }

            // No unload action available for Assembly.Load(bytes)
            return (assembly, null);
        }

        /// <summary>
        /// Get a description of the current loading strategy.
        /// </summary>
        public static string GetStrategyDescription()
        {
#if NETCOREAPP
            return "AssemblyLoadContext (collectible) - Supports true unloading";
#else
            return "Assembly.Load(bytes) - Hot-reload only, no unloading";
#endif
        }
    }
}
