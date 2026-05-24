using System;
using System.IO;

namespace RevitCommandRunner.Utils
{
    /// <summary>
    /// File system utilities for safe file operations.
    /// </summary>
    public static class FileSystemHelper
    {
        /// <summary>
        /// Wait for a file to become available for reading.
        /// Useful when another process is writing the file.
        /// </summary>
        public static bool WaitForFile(string filePath, int timeoutMs = 5000, int retryDelayMs = 100)
        {
            var startTime = DateTime.UtcNow;

            while ((DateTime.UtcNow - startTime).TotalMilliseconds < timeoutMs)
            {
                try
                {
                    using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        return true;
                    }
                }
                catch (IOException)
                {
                    System.Threading.Thread.Sleep(retryDelayMs);
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Safely read a file with retry logic.
        /// </summary>
        public static string ReadFileWithRetry(string filePath, int maxRetries = 3, int retryDelayMs = 100)
        {
            Exception? lastException = null;

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    return File.ReadAllText(filePath);
                }
                catch (IOException ex)
                {
                    lastException = ex;
                    System.Threading.Thread.Sleep(retryDelayMs);
                }
            }

            throw new IOException($"Failed to read file after {maxRetries} retries: {filePath}", lastException);
        }

        /// <summary>
        /// Ensure a directory exists, creating it if necessary.
        /// </summary>
        public static void EnsureDirectoryExists(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        /// <summary>
        /// Get a unique file path by appending a number if the file exists.
        /// </summary>
        public static string GetUniqueFilePath(string basePath)
        {
            if (!File.Exists(basePath))
                return basePath;

            string directory = Path.GetDirectoryName(basePath) ?? "";
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(basePath);
            string extension = Path.GetExtension(basePath);

            int counter = 1;
            string newPath;

            do
            {
                newPath = Path.Combine(directory, $"{fileNameWithoutExt}_{counter}{extension}");
                counter++;
            }
            while (File.Exists(newPath));

            return newPath;
        }
    }
}
