using System;
using System.IO;
using Newtonsoft.Json;

namespace RevitCommandRunner.Models
{
    /// <summary>
    /// Configuration for RevitCommandRunner.
    /// </summary>
    public class RevitCommandRunnerConfig
    {
        /// <summary>
        /// Path to the command queue file that AI agents write to.
        /// </summary>
        [JsonProperty("queueFilePath")]
        public string QueueFilePath { get; set; } = string.Empty;

        /// <summary>
        /// Directory where result files are written.
        /// </summary>
        [JsonProperty("resultsDirectory")]
        public string ResultsDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Maximum execution time in seconds before timeout (0 = no timeout).
        /// </summary>
        [JsonProperty("maxExecutionTimeSeconds")]
        public int MaxExecutionTimeSeconds { get; set; } = 300; // 5 minutes default

        /// <summary>
        /// Whether to capture console output as logs.
        /// </summary>
        [JsonProperty("captureConsoleLogs")]
        public bool CaptureConsoleLogs { get; set; } = true;

        /// <summary>
        /// Whether to delete the queue file after reading it.
        /// </summary>
        [JsonProperty("deleteQueueFileAfterRead")]
        public bool DeleteQueueFileAfterRead { get; set; } = true;

        /// <summary>
        /// Default configuration with standard paths.
        /// </summary>
        public static RevitCommandRunnerConfig Default
        {
            get
            {
                string baseDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "RevitCommandRunner");

                return new RevitCommandRunnerConfig
                {
                    QueueFilePath = Path.Combine(baseDir, "command-queue.json"),
                    ResultsDirectory = Path.Combine(baseDir, "results"),
                    MaxExecutionTimeSeconds = 300,
                    CaptureConsoleLogs = true,
                    DeleteQueueFileAfterRead = true
                };
            }
        }

        /// <summary>
        /// Load configuration from a JSON file.
        /// </summary>
        public static RevitCommandRunnerConfig FromFile(string path)
        {
            if (!File.Exists(path))
                return Default;

            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<RevitCommandRunnerConfig>(json) ?? Default;
        }

        /// <summary>
        /// Save this configuration to a JSON file.
        /// </summary>
        public void SaveToFile(string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        /// <summary>
        /// Get the standard config file path.
        /// </summary>
        public static string GetDefaultConfigPath()
        {
            string baseDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RevitCommandRunner");

            return Path.Combine(baseDir, "config.json");
        }
    }
}
