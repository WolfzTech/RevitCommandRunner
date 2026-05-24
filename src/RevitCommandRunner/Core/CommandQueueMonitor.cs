using System;
using System.IO;
using Autodesk.Revit.UI;
using RevitCommandRunner.Models;

namespace RevitCommandRunner.Core
{
    /// <summary>
    /// Monitors the command queue file and triggers execution via ExternalEvent.
    /// Implements IExternalApplication for Revit add-in lifecycle.
    /// </summary>
    public class CommandQueueMonitor : IExternalApplication
    {
        private FileSystemWatcher? _watcher;
        private ExternalEvent? _externalEvent;
        private CommandExecutor? _executor;
        private RevitCommandRunnerConfig? _config;

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // Load configuration
                string configPath = RevitCommandRunnerConfig.GetDefaultConfigPath();
                _config = RevitCommandRunnerConfig.FromFile(configPath);

                // Ensure directories exist
                Directory.CreateDirectory(Path.GetDirectoryName(_config.QueueFilePath) ?? "");
                Directory.CreateDirectory(_config.ResultsDirectory);

                // Create executor and external event
                _executor = new CommandExecutor(_config);
                _externalEvent = ExternalEvent.Create(_executor);

                // Set up file watcher
                string queueDir = Path.GetDirectoryName(_config.QueueFilePath) ?? "";
                string queueFileName = Path.GetFileName(_config.QueueFilePath);

                _watcher = new FileSystemWatcher(queueDir)
                {
                    Filter = queueFileName,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                    EnableRaisingEvents = true
                };

                _watcher.Changed += OnQueueFileChanged;
                _watcher.Created += OnQueueFileChanged;

                // Silent startup - no dialog
                // TaskDialog.Show("RevitCommandRunner",
                //     $"Command runner started.\nMonitoring: {_config.QueueFilePath}");

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("RevitCommandRunner Error",
                    $"Failed to start: {ex.Message}\n\n{ex.StackTrace}");
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            try
            {
                if (_watcher != null)
                {
                    _watcher.EnableRaisingEvents = false;
                    _watcher.Dispose();
                }

                _externalEvent?.Dispose();

                return Result.Succeeded;
            }
            catch
            {
                return Result.Failed;
            }
        }

        private void OnQueueFileChanged(object sender, FileSystemEventArgs e)
        {
            if (_config == null || _executor == null || _externalEvent == null)
                return;

            try
            {
                // Wait a bit for file write to complete
                System.Threading.Thread.Sleep(100);

                // Read command request
                string json = File.ReadAllText(_config.QueueFilePath);
                var request = CommandRequest.FromJson(json);

                // Delete queue file if configured
                if (_config.DeleteQueueFileAfterRead)
                {
                    try
                    {
                        File.Delete(_config.QueueFilePath);
                    }
                    catch
                    {
                        // Ignore delete errors
                    }
                }

                // Queue command and raise event
                _executor.QueueCommand(request);
                _externalEvent.Raise();
            }
            catch (Exception ex)
            {
                // Write error to results directory
                string errorPath = Path.Combine(_config.ResultsDirectory,
                    $"error-{DateTime.UtcNow:yyyyMMddHHmmss}.txt");
                File.WriteAllText(errorPath, $"Failed to process queue file: {ex}");
            }
        }
    }
}
