using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitCommandRunner.Core;

namespace SamplePlugin
{
    /// <summary>
    /// Reloads specific families from file paths.
    /// Pass family file paths as arguments.
    /// </summary>
    public class ReloadFamilyFromFileCommand : IExternalCommandWithUIApp, ICommandWithData
    {
        private Dictionary<string, object> _customData = new Dictionary<string, object>();

        public Result Execute(UIApplication app, ref string message, ElementSet elements)
        {
            try
            {
                Console.WriteLine("[Info] Starting ReloadFamilyFromFileCommand");
                
                var doc = app.ActiveUIDocument.Document;
                
                if (doc == null)
                {
                    message = "No active document";
                    Console.WriteLine("[Error] No active document");
                    return Result.Failed;
                }

                Console.WriteLine($"[Info] Active document: {doc.Title}");

                // Get family paths from command arguments
                // In RevitCommandRunner, you can pass these via the args parameter
                string[] familyPaths = GetFamilyPathsFromArgs();

                if (familyPaths == null || familyPaths.Length == 0)
                {
                    message = "No family paths provided. Pass family file paths as arguments.";
                    Console.WriteLine("[Error] No family paths provided");
                    return Result.Failed;
                }

                int familiesReloaded = 0;
                int familiesFailed = 0;
                List<string> reloadedFamilies = new List<string>();
                List<string> failedFamilies = new List<string>();

                using (Transaction trans = new Transaction(doc, "Reload Families from Files"))
                {
                    trans.Start();

                    try
                    {
                        foreach (string familyPath in familyPaths)
                        {
                            try
                            {
                                if (!File.Exists(familyPath))
                                {
                                    Console.WriteLine($"[Error] File not found: {familyPath}");
                                    failedFamilies.Add(Path.GetFileName(familyPath));
                                    familiesFailed++;
                                    continue;
                                }

                                Console.WriteLine($"[Info] Loading family from: {familyPath}");

                                // Load/reload the family
                                bool loaded = doc.LoadFamily(familyPath, new FamilyLoadOptions(), out Family family);

                                if (loaded && family != null)
                                {
                                    familiesReloaded++;
                                    reloadedFamilies.Add(family.Name);
                                    Console.WriteLine($"[OK] Reloaded family: {family.Name}");
                                    
                                    // Count instances
                                    int instanceCount = CountFamilyInstances(doc, family);
                                    Console.WriteLine($"[Info] Family has {instanceCount} instances in project");
                                }
                                else
                                {
                                    familiesFailed++;
                                    failedFamilies.Add(Path.GetFileName(familyPath));
                                    Console.WriteLine($"[Error] Failed to load: {familyPath}");
                                }
                            }
                            catch (Exception ex)
                            {
                                familiesFailed++;
                                failedFamilies.Add(Path.GetFileName(familyPath));
                                Console.WriteLine($"[Error] Failed to reload {familyPath}: {ex.Message}");
                            }
                        }

                        trans.Commit();
                        Console.WriteLine($"[Info] Transaction committed");
                    }
                    catch (Exception ex)
                    {
                        trans.RollBack();
                        message = $"Transaction failed: {ex.Message}";
                        Console.WriteLine($"[Error] Transaction failed: {ex.Message}");
                        return Result.Failed;
                    }
                }

                // Store custom data
                _customData["familiesReloaded"] = familiesReloaded;
                _customData["familiesFailed"] = familiesFailed;
                _customData["reloadedFamilies"] = reloadedFamilies.ToArray();
                _customData["failedFamilies"] = failedFamilies.ToArray();
                _customData["documentTitle"] = doc.Title;
                _customData["familyPathsProvided"] = familyPaths.Length;

                message = $"Reloaded {familiesReloaded} families, {familiesFailed} failed";
                Console.WriteLine($"[Success] {message}");

                return familiesFailed == 0 ? Result.Succeeded : Result.Failed;
            }
            catch (Exception ex)
            {
                message = $"Unexpected error: {ex.Message}";
                Console.WriteLine($"[Error] Unexpected error: {ex.Message}");
                Console.WriteLine($"[Error] Stack trace: {ex.StackTrace}");
                return Result.Failed;
            }
        }

        private string[] GetFamilyPathsFromArgs()
        {
            // In a real implementation, these would come from command arguments
            // For now, return a sample path or read from a config file
            
            // Example: Read from environment variable or config
            string pathsEnv = Environment.GetEnvironmentVariable("REVIT_FAMILY_PATHS");
            if (!string.IsNullOrEmpty(pathsEnv))
            {
                return pathsEnv.Split(';');
            }

            // Or return empty to show usage
            return new string[0];
        }

        private int CountFamilyInstances(Document doc, Family family)
        {
            try
            {
                // Get all family symbols for this family
                var symbolIds = family.GetFamilySymbolIds();
                
                int totalInstances = 0;
                foreach (ElementId symbolId in symbolIds)
                {
                    FilteredElementCollector collector = new FilteredElementCollector(doc);
                    int count = collector
                        .OfClass(typeof(FamilyInstance))
                        .Where(fi => ((FamilyInstance)fi).Symbol.Id == symbolId)
                        .Count();
                    
                    totalInstances += count;
                }
                
                return totalInstances;
            }
            catch
            {
                return 0;
            }
        }

        public Dictionary<string, object> GetCustomData()
        {
            return _customData;
        }
    }

    /// <summary>
    /// Options for loading families - always overwrite existing
    /// </summary>
    public class FamilyLoadOptions : IFamilyLoadOptions
    {
        public bool OnFamilyFound(bool familyInUse, out bool overwriteParameterValues)
        {
            // Always overwrite
            overwriteParameterValues = true;
            return true;
        }

        public bool OnSharedFamilyFound(Family sharedFamily, bool familyInUse, out FamilySource source, out bool overwriteParameterValues)
        {
            // Use the new family from file
            source = FamilySource.Family;
            overwriteParameterValues = true;
            return true;
        }
    }
}
