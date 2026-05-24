using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace RevitCommandRunner.Models
{
    /// <summary>
    /// Represents the result of a command execution.
    /// Written to results-{id}.json for AI agents to consume.
    /// </summary>
    public class CommandResult
    {
        /// <summary>
        /// Unique identifier matching the CommandRequest.Id.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Whether the command executed successfully (no exceptions thrown).
        /// </summary>
        [JsonProperty("success")]
        public bool Success { get; set; }

        /// <summary>
        /// Revit Result enum value: "Succeeded", "Failed", or "Cancelled".
        /// </summary>
        [JsonProperty("result")]
        public string Result { get; set; } = string.Empty;

        /// <summary>
        /// Message returned by the command (ref string message parameter).
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Execution time in milliseconds.
        /// </summary>
        [JsonProperty("executionTimeMs")]
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// Timestamp when execution completed.
        /// </summary>
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Log messages captured during execution.
        /// </summary>
        [JsonProperty("logs")]
        public List<string> Logs { get; set; } = new List<string>();

        /// <summary>
        /// Exception details if an error occurred.
        /// </summary>
        [JsonProperty("exception")]
        public ExceptionInfo? Exception { get; set; }

        /// <summary>
        /// Custom data returned by the command (command-specific).
        /// Commands can populate this with verification results, counts, etc.
        /// </summary>
        [JsonProperty("customData")]
        public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Serialize this CommandResult to JSON string.
        /// </summary>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// Write this result to a file.
        /// </summary>
        public void WriteToFile(string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, ToJson());
        }

        /// <summary>
        /// Read a CommandResult from a file.
        /// </summary>
        public static CommandResult FromFile(string path)
        {
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<CommandResult>(json)
                   ?? throw new InvalidOperationException("Failed to deserialize CommandResult");
        }
    }
}
