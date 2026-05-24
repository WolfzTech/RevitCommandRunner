using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitCommandRunner.Models;
using RevitCommandRunner.Utils;

namespace RevitCommandRunner.Core
{
    /// <summary>
    /// Executes commands from DLL files with hot-reload support.
    /// Implements IExternalEventHandler for proper transaction context.
    /// </summary>
    public class CommandExecutor : IExternalEventHandler
    {
        private CommandRequest? _currentRequest;
        private readonly RevitCommandRunnerConfig _config;
        private readonly LogCapture _logCapture;
        private string? _originalDllDirectory;

        public CommandExecutor(RevitCommandRunnerConfig config)
        {
            _config = config;
            _logCapture = new LogCapture();

            // Set up assembly resolver for dependencies
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        }

        private Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
        {
            // When loading a command DLL from temp, dependencies won't be found
            // Look for them in the original DLL's directory
            if (_originalDllDirectory == null)
                return null;

            var assemblyName = new AssemblyName(args.Name);
            var dllFileName = assemblyName.Name + ".dll";
            var dllPath = Path.Combine(_originalDllDirectory, dllFileName);

            if (File.Exists(dllPath))
            {
                try
                {
                    return Assembly.LoadFrom(dllPath);
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        /// <summary>
        /// Queue a command for execution. Call ExternalEvent.Raise() after this.
        /// </summary>
        public void QueueCommand(CommandRequest request)
        {
            _currentRequest = request;
        }

        /// <summary>
        /// Execute the queued command. Called by Revit's ExternalEvent mechanism.
        /// </summary>
        public void Execute(UIApplication app)
        {
            if (_currentRequest == null)
                return;

            var request = _currentRequest;
            _currentRequest = null;

            var result = new CommandResult
            {
                Id = request.Id,
                Timestamp = DateTime.UtcNow
            };

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Validate request
                if (!request.IsValid(out string validationError))
                {
                    result.Success = false;
                    result.Result = "Failed";
                    result.Message = $"Invalid request: {validationError}";
                    WriteResult(result);
                    return;
                }

                // Start log capture
                if (_config.CaptureConsoleLogs)
                    _logCapture.Start();

                // Load and execute command
                var commandResult = ExecuteCommand(app, request);

                result.Success = commandResult.Success;
                result.Result = commandResult.ResultEnum;
                result.Message = commandResult.Message;
                result.CustomData = commandResult.CustomData;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Result = "Failed";
                result.Message = $"Execution failed: {ex.Message}";
                result.Exception = ExceptionInfo.FromException(ex);
            }
            finally
            {
                stopwatch.Stop();
                result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

                // Capture logs
                if (_config.CaptureConsoleLogs)
                {
                    _logCapture.Stop();
                    result.Logs = _logCapture.GetLogs();
                }

                WriteResult(result);
            }
        }

        private CommandExecutionResult ExecuteCommand(UIApplication app, CommandRequest request)
        {
            var result = new CommandExecutionResult();

            // Store original DLL directory for dependency resolution
            _originalDllDirectory = Path.GetDirectoryName(request.DllPath);

            // Copy DLL to temp location for hot-reload
            string tempDllPath = AssemblyLoader.CopyToTempWithTimestamp(request.DllPath);

            Action? unloadAssembly = null;
            try
            {
                var loadResult = AssemblyLoaderV2.LoadAssembly(tempDllPath);
                Assembly assembly = loadResult.Assembly;
                unloadAssembly = loadResult.Unload;

                Console.WriteLine($"[Info] Assembly loading strategy: {AssemblyLoaderV2.GetStrategyDescription()}");

                // Find command type
                Type? commandType = assembly.GetTypes()
                    .FirstOrDefault(t => t.FullName == request.CommandClassName);

                if (commandType == null)
                {
                    result.Success = false;
                    result.ResultEnum = "Failed";
                    result.Message = $"Command class '{request.CommandClassName}' not found in assembly";
                    return result;
                }

                // Check if command implements IExternalCommandWithUIApp (alternative interface)
                if (typeof(IExternalCommandWithUIApp).IsAssignableFrom(commandType))
                {
                    var uiAppCommand = Activator.CreateInstance(commandType) as IExternalCommandWithUIApp;
                    if (uiAppCommand == null)
                    {
                        result.Success = false;
                        result.ResultEnum = "Failed";
                        result.Message = $"Failed to instantiate command '{request.CommandClassName}'";
                        return result;
                    }

                    // Execute directly with UIApplication
                    string message = string.Empty;
                    ElementSet elementSet = new ElementSet();
                    var revitResult = uiAppCommand.Execute(app, ref message, elementSet);

                    result.Success = true;
                    result.ResultEnum = revitResult.ToString();
                    result.Message = message ?? string.Empty;

                    // Store custom data if command implements ICommandWithData
                    if (uiAppCommand is ICommandWithData dataCommand)
                    {
                        result.CustomData = dataCommand.GetCustomData();
                    }
                }
                // Fall back to IExternalCommand
                else if (typeof(IExternalCommand).IsAssignableFrom(commandType))
                {
                    var command = Activator.CreateInstance(commandType) as IExternalCommand;
                    if (command == null)
                    {
                        result.Success = false;
                        result.ResultEnum = "Failed";
                        result.Message = $"Failed to instantiate command '{request.CommandClassName}'";
                        return result;
                    }

                    // Execute command using a wrapper that creates ExternalCommandData
                    var wrapper = new CommandWrapper(app, command);
                    var revitResult = wrapper.Execute();

                    result.Success = true;
                    result.ResultEnum = revitResult.Result.ToString();
                    result.Message = revitResult.Message ?? string.Empty;

                    // Store custom data if command implements ICommandWithData
                    if (command is ICommandWithData dataCommand)
                    {
                        result.CustomData = dataCommand.GetCustomData();
                    }
                }
                else
                {
                    result.Success = false;
                    result.ResultEnum = "Failed";
                    result.Message = $"Class '{request.CommandClassName}' does not implement IExternalCommand or IExternalCommandWithUIApp";
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ResultEnum = "Failed";
                result.Message = ex.Message;
                result.Exception = ExceptionInfo.FromException(ex);
            }
            finally
            {
                unloadAssembly?.Invoke();
            }

            return result;
        }

        /// <summary>
        /// Wrapper that executes an IExternalCommand with proper ExternalCommandData.
        /// </summary>
        private class CommandWrapper
        {
            private readonly UIApplication _app;
            private readonly IExternalCommand _command;

            public CommandWrapper(UIApplication app, IExternalCommand command)
            {
                _app = app;
                _command = command;
            }

            public CommandExecutionResult Execute()
            {
                var result = new CommandExecutionResult();

                try
                {
                    // Build ExternalCommandData using reflection
                    var commandData = BuildExternalCommandData();
                    if (commandData == null)
                    {
                        result.Success = false;
                        result.ResultEnum = "Failed";
                        result.Message = "Failed to create ExternalCommandData. This may be a Revit API limitation.";
                        return result;
                    }

                    // Execute the command
                    string message = string.Empty;
                    ElementSet elementSet = new ElementSet();

                    var revitResult = _command.Execute(commandData, ref message, elementSet);

                    result.Success = true;
                    result.ResultEnum = revitResult.ToString();
                    result.Message = message ?? string.Empty;
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.ResultEnum = "Failed";
                    result.Message = ex.Message;
                    result.Exception = ExceptionInfo.FromException(ex);
                }

                return result;
            }

            private ExternalCommandData? BuildExternalCommandData()
            {
                var uiDoc = _app.ActiveUIDocument;
                if (uiDoc == null)
                    return null;

                var doc = uiDoc.Document;
                var view = uiDoc.ActiveView;
                var type = typeof(ExternalCommandData);

                // Try all constructors with all parameter permutations
                var constructors = type.GetConstructors(
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                foreach (var ctor in constructors)
                {
                    var parameters = ctor.GetParameters();
                    if (parameters.Length == 3)
                    {
                        var combinations = new object[][]
                        {
                            new object[] { _app, view, doc },
                            new object[] { _app, doc, view },
                            new object[] { view, _app, doc },
                            new object[] { view, doc, _app },
                            new object[] { doc, _app, view },
                            new object[] { doc, view, _app }
                        };

                        foreach (var combo in combinations)
                        {
                            try
                            {
                                var result = ctor.Invoke(combo) as ExternalCommandData;
                                if (result != null && result.Application != null)
                                {
                                    return result;
                                }
                            }
                            catch { }
                        }
                    }
                }

                return null;
            }
        }

        private class CommandExecutionResult
        {
            public bool Success { get; set; }
            public string ResultEnum { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public ExceptionInfo? Exception { get; set; }
            public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();
            public Result Result => Enum.TryParse<Result>(ResultEnum, out var r) ? r : Autodesk.Revit.UI.Result.Failed;
        }

        private void WriteResult(CommandResult result)
        {
            try
            {
                string resultPath = Path.Combine(_config.ResultsDirectory, $"results-{result.Id}.json");
                result.WriteToFile(resultPath);
            }
            catch (Exception ex)
            {
                // Log to file if result write fails
                string errorLog = Path.Combine(_config.ResultsDirectory, $"error-{result.Id}.txt");
                File.WriteAllText(errorLog, $"Failed to write result: {ex}");
            }
        }

        public string GetName() => "RevitCommandRunner.CommandExecutor";
    }

    /// <summary>
    /// Optional interface for commands that want to return custom data.
    /// </summary>
    public interface ICommandWithData
    {
        Dictionary<string, object> GetCustomData();
    }

    /// <summary>
    /// Alternative interface for commands that work directly with UIApplication.
    /// Use this when ExternalCommandData construction fails or is not needed.
    /// </summary>
    public interface IExternalCommandWithUIApp
    {
        Result Execute(UIApplication app, ref string message, ElementSet elements);
    }
}
