using System;
using System.IO;

namespace RevitCommandRunner.Utils
{
    /// <summary>
    /// Utilities for loading assemblies with hot-reload support.
    /// </summary>
    public static class AssemblyLoader
    {
        /// <summary>
        /// Copy a DLL to a temp location with timestamp for hot-reload.
        /// This allows the original DLL to be rebuilt without locking.
        /// </summary>
        public static string CopyToTempWithTimestamp(string originalDllPath)
        {
            if (!File.Exists(originalDllPath))
                throw new FileNotFoundException($"DLL not found: {originalDllPath}");

            string fileName = Path.GetFileNameWithoutExtension(originalDllPath);
            string extension = Path.GetExtension(originalDllPath);
            string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");

            string tempDir = Path.Combine(Path.GetTempPath(), "RevitCommandRunner");
            Directory.CreateDirectory(tempDir);

            string tempDllPath = Path.Combine(tempDir, $"{fileName}_{timestamp}{extension}");

            File.Copy(originalDllPath, tempDllPath, overwrite: true);

            // Also copy PDB if exists for better debugging
            string originalPdbPath = Path.ChangeExtension(originalDllPath, ".pdb");
            if (File.Exists(originalPdbPath))
            {
                string tempPdbPath = Path.ChangeExtension(tempDllPath, ".pdb");
                File.Copy(originalPdbPath, tempPdbPath, overwrite: true);
            }

            return tempDllPath;
        }

        /// <summary>
        /// Clean up old temp DLLs to prevent disk bloat.
        /// Call this periodically or on shutdown.
        /// </summary>
        public static void CleanupOldTempFiles(TimeSpan olderThan)
        {
            try
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "RevitCommandRunner");
                if (!Directory.Exists(tempDir))
                    return;

                var cutoffTime = DateTime.UtcNow - olderThan;

                foreach (string file in Directory.GetFiles(tempDir))
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.CreationTimeUtc < cutoffTime)
                        {
                            File.Delete(file);
                        }
                    }
                    catch
                    {
                        // Ignore errors deleting individual files
                    }
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
