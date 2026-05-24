using System;
using Newtonsoft.Json;

namespace RevitCommandRunner.Models
{
    /// <summary>
    /// Captures exception details in a serializable format.
    /// </summary>
    public class ExceptionInfo
    {
        /// <summary>
        /// Full type name of the exception (e.g., "System.InvalidOperationException").
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Exception message.
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Stack trace showing where the exception occurred.
        /// </summary>
        [JsonProperty("stackTrace")]
        public string StackTrace { get; set; } = string.Empty;

        /// <summary>
        /// Inner exception details, if any.
        /// </summary>
        [JsonProperty("innerException")]
        public ExceptionInfo? InnerException { get; set; }

        /// <summary>
        /// Create ExceptionInfo from an Exception object.
        /// </summary>
        public static ExceptionInfo FromException(Exception ex)
        {
            return new ExceptionInfo
            {
                Type = ex.GetType().FullName ?? ex.GetType().Name,
                Message = ex.Message,
                StackTrace = ex.StackTrace ?? string.Empty,
                InnerException = ex.InnerException != null ? FromException(ex.InnerException) : null
            };
        }
    }
}
