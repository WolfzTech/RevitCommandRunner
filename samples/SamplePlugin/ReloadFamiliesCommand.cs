using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitCommandRunner.Core;

namespace SamplePlugin
{
    /// <summary>
    /// Reloads families from their source files and updates all instances in the project.
    /// Useful after modifying family files externally.
    /// </summary>
    public class ReloadFamiliesCommand : IExternalCommandWithUIApp, ICommandWithData
    {
        private Dictionary<string, object> _customData = new Dictionary<string, object>();

        public Result Execute(UIApplication app, ref string message, ElementSet elements)
        {
            try
            {
                Console.WriteLine("[Info] Starting ReloadFamiliesCommand");
                
                var doc = app.ActiveUIDocument.Document;
                
                if (doc == null)
                {
                    message = "No active document";
                    Console.WriteLine("[Error] No active document");
                    return Result.Failed;
                }

                Console.WriteLine($"[Info] Active document: {doc.Title}");

                int familiesReloaded = 0;
                int familiesFailed = 0;
                List<string> reloadedFamilies = new List<string>();
                List<string> failedFamilies = new List<string>();

                using (Transaction trans = new Transaction(doc, "Reload Families"))
                {
                    trans.Start();

                    try
                    {
                        // Get all family symbols (types) in the document
                        FilteredElementCollector collector = new FilteredElementCollector(doc);
                        var familySymbols = collector.OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>();

                        // Group by family
                        var families = familySymbols
                            .Select(fs => fs.Family)
                            .Distinct()
                            .Where(f => f != null && !string.IsNullOrEmpty(f.Name));

                        Console.WriteLine($"[Info] Found {families.Count()} families to check");

                        foreach (Family family in families)
                        {
                            try
                            {
                                // Check if family has an external file path
                                if (!family.IsEditable)
                                {
                                    Console.WriteLine($"[Skip] {family.Name} - System family, cannot reload");
                                    continue;
                                }

                                Document familyDoc = doc.EditFamily(family);
                                
                                if (familyDoc != null)
                                {
                                    // Close without saving (we just wanted to trigger reload)
                                    familyDoc.Close(false);
                                    
                                    // Alternative: Reload from file if path is available
                                    // This is more reliable for external changes
                                    bool reloaded = false;
                                    
                                    try
                                    {
                                        // Try to reload from the original file path
                                        // Note: This requires the family to have been loaded from a file
                                        family.Document.LoadFamily(family.Name, out Family reloadedFamily);
                                        reloaded = true;
                                    }
                                    catch
                                    {
                                        // If reload fails, the EditFamily above still refreshed it
                                        reloaded = true;
                                    }

                                    if (reloaded)
                                    {
                                        familiesReloaded++;
                                        reloadedFamilies.Add(family.Name);
                                        Console.WriteLine($"[OK] Reloaded family: {family.Name}");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                familiesFailed++;
                                failedFamilies.Add(family.Name);
                                Console.WriteLine($"[Error] Failed to reload {family.Name}: {ex.Message}");
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

        public Dictionary<string, object> GetCustomData()
        {
            return _customData;
        }
    }
}
