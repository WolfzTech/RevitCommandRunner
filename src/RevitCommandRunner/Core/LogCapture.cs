using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RevitCommandRunner.Core
{
    /// <summary>
    /// Captures console output (stdout/stderr) during command execution.
    /// </summary>
    public class LogCapture
    {
        private readonly List<string> _logs = new List<string>();
        private readonly StringWriter _stringWriter;
        private readonly TextWriter _originalOut;
        private readonly TextWriter _originalError;
        private bool _isCapturing;

        public LogCapture()
        {
            _stringWriter = new StringWriter();
            _originalOut = Console.Out;
            _originalError = Console.Error;
        }

        /// <summary>
        /// Start capturing console output.
        /// </summary>
        public void Start()
        {
            if (_isCapturing)
                return;

            _logs.Clear();
            Console.SetOut(_stringWriter);
            Console.SetError(_stringWriter);
            _isCapturing = true;
        }

        /// <summary>
        /// Stop capturing and restore original console output.
        /// </summary>
        public void Stop()
        {
            if (!_isCapturing)
                return;

            Console.SetOut(_originalOut);
            Console.SetError(_originalError);
            _isCapturing = false;

            // Parse captured output into log lines
            string captured = _stringWriter.ToString();
            if (!string.IsNullOrEmpty(captured))
            {
                _logs.AddRange(captured.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
            }
        }

        /// <summary>
        /// Get all captured log lines.
        /// </summary>
        public List<string> GetLogs()
        {
            return new List<string>(_logs);
        }

        /// <summary>
        /// Add a log line manually (for framework messages).
        /// </summary>
        public void AddLog(string message)
        {
            _logs.Add(message);
        }
    }
}
