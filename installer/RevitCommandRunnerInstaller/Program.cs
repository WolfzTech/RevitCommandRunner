namespace RevitCommandRunnerInstaller;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

internal static class Program
{
    private const string BundleName = "RevitCommandRunner.bundle";
    private const string AppName = "RevitCommandRunner";
    private const string AppVersion = "1.0.2";
    private const string Publisher = "WolfzTech";
    private const string UninstallRegKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\RevitCommandRunner";

    [STAThread]
    private static int Main(string[] args)
    {
        var uninstall = args.Any(a => a.Equals("/uninstall", StringComparison.OrdinalIgnoreCase)
                                  || a.Equals("--uninstall", StringComparison.OrdinalIgnoreCase)
                                  || a.Equals("-u", StringComparison.OrdinalIgnoreCase));

        var app = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
        var window = new InstallerWindow(uninstall);
        app.Run(window);
        return window.ExitCode;
    }

    private sealed class InstallerWindow : Window
    {
        private readonly bool _uninstall;
        private readonly ListBox _log = new();
        private readonly CheckBox _claudeCode = new() { Content = "Claude Code", IsChecked = true };
        private readonly CheckBox _claudeDesktop = new() { Content = "Claude Desktop", IsChecked = true };
        private readonly CheckBox _openCode = new() { Content = "OpenCode", IsChecked = true };
        private readonly CheckBox _antigravity = new() { Content = "Antigravity", IsChecked = true };
        private readonly Button _primary = new() { Width = 110, Height = 30 };
        private readonly Button _close = new() { Content = "Close", Width = 90, Height = 30, IsEnabled = true };

        public int ExitCode { get; private set; }

        public InstallerWindow(bool uninstall)
        {
            _uninstall = uninstall;
            Title = uninstall ? "RevitCommandRunner Uninstaller" : "RevitCommandRunner Installer";
            Width = 620;
            Height = uninstall ? 390 : 470;
            MinWidth = 560;
            MinHeight = uninstall ? 340 : 430;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var root = new Grid { Margin = new Thickness(18) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var title = new TextBlock
            {
                Text = uninstall ? "Uninstall RevitCommandRunner" : "Install RevitCommandRunner",
                FontSize = 20,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 12)
            };
            root.Children.Add(title);

            if (!uninstall)
            {
                var mcpBox = new GroupBox { Header = "Configure MCP clients", Margin = new Thickness(0, 0, 0, 12) };
                var checks = new StackPanel { Margin = new Thickness(10) };
                checks.Children.Add(new TextBlock { Text = "Selected clients will receive or update the revit-command-runner MCP config.", Margin = new Thickness(0, 0, 0, 8) });
                checks.Children.Add(_claudeCode);
                checks.Children.Add(_claudeDesktop);
                checks.Children.Add(_openCode);
                checks.Children.Add(_antigravity);
                mcpBox.Content = checks;
                Grid.SetRow(mcpBox, 1);
                root.Children.Add(mcpBox);
            }

            _log.Margin = new Thickness(0, 0, 0, 12);
            _log.Focusable = false;
            Grid.SetRow(_log, 2);
            root.Children.Add(_log);

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            _primary.Content = uninstall ? "Uninstall" : "Install";
            _primary.Margin = new Thickness(0, 0, 10, 0);
            _primary.Click += async (_, _) => await RunAsync();
            _close.Click += (_, _) => Close();
            buttons.Children.Add(_primary);
            buttons.Children.Add(_close);
            Grid.SetRow(buttons, 3);
            root.Children.Add(buttons);

            Content = root;
        }

        private async System.Threading.Tasks.Task RunAsync()
        {
            _primary.IsEnabled = false;
            _close.IsEnabled = false;
            ExitCode = 0;

            try
            {
                await System.Threading.Tasks.Task.Run(() =>
                {
                    var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    var applicationPluginsRoot = Path.Combine(appData, "Autodesk", "ApplicationPlugins");
                    var destinationBundle = Path.Combine(applicationPluginsRoot, BundleName);

                    if (_uninstall)
                    {
                        Uninstall(destinationBundle, Log);
                    }
                    else
                    {
                        Install(applicationPluginsRoot, destinationBundle, Log);
                    }
                });

                if (!_uninstall)
                    ConfigureSelectedMcpClients();

                Log(_uninstall ? "Uninstall completed." : "Installation completed. Start Revit to load RevitCommandRunner.");
            }
            catch (Exception ex)
            {
                ExitCode = 1;
                Log("ERROR: " + ex.Message);
            }
            finally
            {
                _close.IsEnabled = true;
            }
        }

        private void ConfigureSelectedMcpClients()
        {
            if (_claudeCode.IsChecked == true)
            {
                TryConfigureClient("Claude Code", UpsertClaudeCode);
            }
            if (_claudeDesktop.IsChecked == true)
            {
                TryConfigureClient("Claude Desktop", UpsertClaudeDesktop);
            }
            if (_openCode.IsChecked == true)
            {
                TryConfigureClient("OpenCode", UpsertOpenCode);
            }
            if (_antigravity.IsChecked == true)
            {
                TryConfigureClient("Antigravity", UpsertAntigravity);
            }
        }

        private void TryConfigureClient(string name, Action configure)
        {
            try
            {
                configure();
                Log($"MCP: {name} configured.");
            }
            catch (Exception ex)
            {
                Log($"MCP: {name} skipped. Config file could not be parsed: {ex.Message}");
            }
        }

        private void Log(string message)
        {
            Dispatcher.Invoke(() =>
            {
                _log.Items.Add(message);
                _log.ScrollIntoView(message);
            });
        }
    }

    private static void Install(string applicationPluginsRoot, string destinationBundle, Action<string> log)
    {
        log("Extracting embedded bundle...");
        ExtractEmbeddedBundle(applicationPluginsRoot, destinationBundle, log);
        RegisterInControlPanel(destinationBundle);
        log("Registered in Programs & Features.");
    }

    private static void Uninstall(string destinationBundle, Action<string> log)
    {
        if (Directory.Exists(destinationBundle))
        {
            Directory.Delete(destinationBundle, recursive: true);
            log("Bundle uninstalled.");
        }
        else
        {
            log("Bundle was not installed.");
        }

        UnregisterFromControlPanel();
        log("Unregistered from Programs & Features.");
    }

    private static void ExtractEmbeddedBundle(string applicationPluginsRoot, string destinationBundle, Action<string> log)
    {
        string? tempZip = null;
        try
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames().FirstOrDefault(r => r.EndsWith("bundle.zip", StringComparison.OrdinalIgnoreCase));
            if (resourceName == null)
                throw new InvalidOperationException("Embedded bundle.zip was not found.");

            Directory.CreateDirectory(applicationPluginsRoot);

            if (Directory.Exists(destinationBundle))
            {
                try
                {
                    Directory.Delete(destinationBundle, recursive: true);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Cannot replace existing bundle. Close Revit and retry. Details: {ex.Message}");
                }
            }

            using var stream = assembly.GetManifestResourceStream(resourceName) ?? throw new InvalidOperationException("Embedded bundle stream could not be opened.");
            tempZip = Path.Combine(Path.GetTempPath(), $"RevitCommandRunner-bundle-{Guid.NewGuid():N}.zip");
            using (var fileStream = File.Create(tempZip))
            {
                stream.CopyTo(fileStream);
            }

            ZipFile.ExtractToDirectory(tempZip, applicationPluginsRoot, overwriteFiles: true);

            if (!Directory.Exists(destinationBundle))
                throw new InvalidOperationException("Bundle extraction failed.");

            log($"Bundle installed to {destinationBundle}");
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(tempZip) && File.Exists(tempZip))
            {
                try { File.Delete(tempZip); } catch { }
            }
        }
    }

    private static void RegisterInControlPanel(string installPath)
    {
        using var key = Registry.CurrentUser.CreateSubKey(UninstallRegKey);
        if (key == null) return;

        key.SetValue("DisplayName", AppName);
        key.SetValue("DisplayVersion", AppVersion);
        key.SetValue("Publisher", Publisher);
        key.SetValue("InstallLocation", installPath);
        key.SetValue("UninstallString", $"\"{Environment.ProcessPath}\" --uninstall");
        key.SetValue("DisplayIcon", Environment.ProcessPath ?? "");
        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
        key.SetValue("EstimatedSize", (int)(GetDirectorySize(installPath) / 1024), RegistryValueKind.DWord);
    }

    private static void UnregisterFromControlPanel()
    {
        try { Registry.CurrentUser.DeleteSubKey(UninstallRegKey, throwOnMissingSubKey: false); } catch { }
    }

    private static long GetDirectorySize(string path)
    {
        if (!Directory.Exists(path)) return 0;
        long size = 0;
        foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
        {
            try { size += new FileInfo(file).Length; } catch { }
        }
        return size;
    }

    private static string InstalledMcpServerPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Autodesk",
            "ApplicationPlugins",
            "RevitCommandRunner.bundle",
            "mcp-server",
            "index.js").Replace("\\", "/");
    }

    private static JsonObject BuildNodeServerNode()
    {
        return new JsonObject
        {
            ["type"] = "stdio",
            ["command"] = "node",
            ["args"] = new JsonArray(InstalledMcpServerPath())
        };
    }

    private static JsonObject BuildAntigravityServerNode()
    {
        return new JsonObject
        {
            ["type"] = "stdio",
            ["command"] = "cmd",
            ["args"] = new JsonArray("/c", "node", "%APPDATA%\\Autodesk\\ApplicationPlugins\\RevitCommandRunner.bundle\\mcp-server\\index.js")
        };
    }

    private static JsonObject BuildOpenCodeServerNode()
    {
        return new JsonObject
        {
            ["type"] = "local",
            ["command"] = new JsonArray("node", InstalledMcpServerPath()),
            ["enabled"] = true
        };
    }

    private static JsonObject BuildClaudeDesktopServerNode()
    {
        return new JsonObject
        {
            ["command"] = "node",
            ["args"] = new JsonArray(InstalledMcpServerPath())
        };
    }

    private static JsonObject LoadJsonObject(string path, bool jsonc)
    {
        if (!File.Exists(path)) return new JsonObject();
        var raw = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(raw)) return new JsonObject();

        var documentOptions = jsonc
            ? new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip }
            : default;

        return JsonNode.Parse(raw, nodeOptions: null, documentOptions)?.AsObject() ?? new JsonObject();
    }

    private static void SaveJson(string path, JsonObject root)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    private static JsonObject EnsureObject(JsonObject parent, string key)
    {
        if (parent[key] is JsonObject o) return o;
        o = new JsonObject();
        parent[key] = o;
        return o;
    }

    private static void UpsertClaudeCode()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude.json");
        var root = LoadJsonObject(path, jsonc: true);
        var mcpServers = EnsureObject(root, "mcpServers");
        mcpServers["revit-command-runner"] = BuildNodeServerNode();
        SaveJson(path, root);
    }

    private static void UpsertOpenCode()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "opencode", "opencode.jsonc");
        var root = LoadJsonObject(path, jsonc: true);
        var mcp = EnsureObject(root, "mcp");
        mcp["revit-command-runner"] = BuildOpenCodeServerNode();
        SaveJson(path, root);
    }

    private static void UpsertClaudeDesktop()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Claude", "claude_desktop_config.json");
        var root = LoadJsonObject(path, jsonc: true);
        var mcpServers = EnsureObject(root, "mcpServers");
        mcpServers["revit-command-runner"] = BuildClaudeDesktopServerNode();
        SaveJson(path, root);
    }

    private static void UpsertAntigravity()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gemini", "antigravity", "mcp_config.json");
        var root = LoadJsonObject(path, jsonc: true);
        var mcpServers = EnsureObject(root, "mcpServers");
        mcpServers["revit-command-runner"] = BuildAntigravityServerNode();
        SaveJson(path, root);
    }
}
