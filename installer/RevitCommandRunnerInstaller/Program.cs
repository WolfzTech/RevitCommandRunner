namespace RevitCommandRunnerInstaller;

internal static class Program
{
    private const string BundleName = "RevitCommandRunner.bundle";
    private const string AppName = "RevitCommandRunner";
    private const string AppVersion = "1.0.0";
    private const string Publisher = "WolfzTech";
    private const string UninstallRegKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\RevitCommandRunner";

    private static int Main(string[] args)
    {
        Console.Title = "RevitCommandRunner Installer";
        bool uninstall = args.Any(a => a.Equals("/uninstall", StringComparison.OrdinalIgnoreCase)
                                    || a.Equals("--uninstall", StringComparison.OrdinalIgnoreCase)
                                    || a.Equals("-u", StringComparison.OrdinalIgnoreCase));

        PrintHeader(uninstall);

        string baseDir = AppContext.BaseDirectory;
        string sourceBundle = FindBundleDirectory(baseDir);
        string sourcePackageContents = FindPackageContentsPath(baseDir);

        if (!uninstall)
        {
            if (!Directory.Exists(sourceBundle))
                return Fail("Bundle not found. Run Installer.exe from the extracted release folder, or build the repo bundle first.");

            if (!File.Exists(sourcePackageContents))
                return Fail("PackageContents.xml not found. Run Installer.exe from the extracted release folder.");
        }

        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string applicationPluginsRoot = Path.Combine(appData, "Autodesk", "ApplicationPlugins");
        string destinationBundle = Path.Combine(applicationPluginsRoot, BundleName);

        try
        {
            if (uninstall)
            {
                if (Directory.Exists(destinationBundle))
                {
                    Directory.Delete(destinationBundle, recursive: true);
                    WriteStatus("Bundle", "Uninstalled", ConsoleColor.Green);
                }
                else
                {
                    WriteStatus("Bundle", "Not installed", ConsoleColor.DarkGray);
                }
                
                // Unregister from Windows Programs & Features
                UnregisterFromControlPanel();
                WriteStatus("Registry", "Unregistered from Programs & Features", ConsoleColor.Green);
            }
            else
            {
                Directory.CreateDirectory(applicationPluginsRoot);

                if (Directory.Exists(destinationBundle))
                    Directory.Delete(destinationBundle, recursive: true);

                CopyDirectory(sourceBundle, destinationBundle);
                File.Copy(sourcePackageContents, Path.Combine(destinationBundle, "PackageContents.xml"), overwrite: true);

                WriteStatus("Bundle", $"Installed to {destinationBundle}", ConsoleColor.Green);
                
                // Register in Windows Programs & Features
                RegisterInControlPanel(destinationBundle);
                WriteStatus("Registry", "Registered in Programs & Features", ConsoleColor.Green);
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(uninstall ? "Uninstall completed." : "Installation completed. Start Revit to load RevitCommandRunner.");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }

        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey(intercept: true);

        return 0;
    }

    private static void RegisterInControlPanel(string installPath)
    {
        using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(UninstallRegKey);
        if (key == null) return;

        key.SetValue("DisplayName", AppName);
        key.SetValue("DisplayVersion", AppVersion);
        key.SetValue("Publisher", Publisher);
        key.SetValue("InstallLocation", installPath);
        key.SetValue("UninstallString", $"\"{Environment.ProcessPath}\" --uninstall");
        key.SetValue("DisplayIcon", Environment.ProcessPath ?? "");
        key.SetValue("NoModify", 1, Microsoft.Win32.RegistryValueKind.DWord);
        key.SetValue("NoRepair", 1, Microsoft.Win32.RegistryValueKind.DWord);
        
        // Calculate size
        long size = GetDirectorySize(installPath);
        key.SetValue("EstimatedSize", (int)(size / 1024), Microsoft.Win32.RegistryValueKind.DWord); // Size in KB
    }

    private static void UnregisterFromControlPanel()
    {
        try
        {
            Microsoft.Win32.Registry.CurrentUser.DeleteSubKey(UninstallRegKey, throwOnMissingSubKey: false);
        }
        catch
        {
            // Ignore errors during unregister
        }
    }

    private static long GetDirectorySize(string path)
    {
        if (!Directory.Exists(path)) return 0;
        
        long size = 0;
        foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
        {
            try
            {
                size += new FileInfo(file).Length;
            }
            catch
            {
                // Ignore inaccessible files
            }
        }
        return size;
    }

    private static string FindBundleDirectory(string baseDir)
    {
        string[] candidates =
        [
            Path.Combine(baseDir, BundleName),
            Path.GetFullPath(Path.Combine(baseDir, "..", "build", BundleName)),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "build", BundleName)),
        ];

        return candidates.FirstOrDefault(Directory.Exists) ?? candidates[0];
    }

    private static string FindPackageContentsPath(string baseDir)
    {
        string[] candidates =
        [
            Path.Combine(baseDir, BundleName, "PackageContents.xml"),
            Path.Combine(baseDir, "PackageContents.xml"),
            Path.GetFullPath(Path.Combine(baseDir, "..", "PackageContents.xml")),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "installer", "PackageContents.xml")),
        ];

        return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        Directory.CreateDirectory(destinationDir);

        foreach (string directory in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(sourceDir, directory);
            Directory.CreateDirectory(Path.Combine(destinationDir, relative));
        }

        foreach (string file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(sourceDir, file);
            File.Copy(file, Path.Combine(destinationDir, relative), overwrite: true);
        }
    }

    private static void PrintHeader(bool uninstall)
    {
        Console.WriteLine("========================================");
        Console.WriteLine(uninstall ? "RevitCommandRunner Uninstaller" : "RevitCommandRunner Installer");
        Console.WriteLine("========================================");
        Console.WriteLine();
    }

    private static void WriteStatus(string target, string message, ConsoleColor color)
    {
        Console.Write($"  {target} - ");
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    private static int Fail(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey(intercept: true);
        return 1;
    }
}
