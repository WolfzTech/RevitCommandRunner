using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RevitCommandRunner.Models
{
    /// <summary>
    /// Represents a command execution request from an AI agent or external tool.
    /// </summary>
    public class CommandRequest
    {
        /// <summary>
        /// Unique identifier for this command execution (e.g., "run-001").
        /// Used to correlate request with result file.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the request was created.
        /// </summary>
        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Full path to the DLL containing the command to execute.
        /// </summary>
        [JsonProperty("dll")]
        public string DllPath { get; set; } = string.Empty;

        /// <summary>
        /// Full class name of the command (e.g., "MyNamespace.MyCommand").
        /// Must implement IExternalCommand.
        /// </summary>
        [JsonProperty("command")]
        public string CommandClassName { get; set; } = string.Empty;

        /// <summary>
        /// Optional arguments to pass to the command.
        /// How these are used depends on the command implementation.
        /// </summary>
        [JsonProperty("args")]
        public List<string> Args { get; set; } = new List<string>();

        /// <summary>
        /// Optional metadata for tracking, logging, or custom purposes.
        /// </summary>
        [JsonProperty("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Deserialize a CommandRequest from JSON string.
        /// </summary>
        public static CommandRequest FromJson(string json)
        {
            return JsonConvert.DeserializeObject<CommandRequest>(json)
                   ?? throw new InvalidOperationException("Failed to deserialize CommandRequest");
        }

        /// <summary>
        /// Serialize this CommandRequest to JSON string.
        /// </summary>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// Validate that all required fields are present.
        /// </summary>
        public bool IsValid(out string error)
        {
            if (string.IsNullOrWhiteSpace(Id))
            {
                error = "Id is required";
                return false;
            }

            if (string.IsNullOrWhiteSpace(DllPath))
            {
                error = "DllPath is required";
                return false;
            }

            if (string.IsNullOrWhiteSpace(CommandClassName))
            {
                error = "CommandClassName is required";
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}
