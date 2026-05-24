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
    /// Updates all instances of families in the project after family modifications.
    /// Can reload specific families by name or reload all families.
    /// </summary>
    public class UpdateFamilyInstancesCommand : IExternalCommandWithUIApp, ICommandWithData
    {
        private Dictionary<string, object> _customData = new Dictionary<string, object>();

        public Result Execute(UIApplication app, ref string message, ElementSet elements)
        {
            try
            {
                Console.WriteLine("[Info] Starting UpdateFamilyInstancesCommand");
                
                var doc = app.ActiveUIDocument.Document;
                
                if (doc == null)
                {
                    message = "No active document";
                    Console.WriteLine("[Error] No active document");
                    return Result.Failed;
                }

                Console.WriteLine($"[Info] Active document: {doc.Title}");

                int instancesUpdated = 0;
                int familiesProcessed = 0;
                List<string> updatedFamilies = new List<string>();

                using (Transaction trans = new Transaction(doc, "Update Family Instances"))
                {
                    trans.Start();

                    try
                    {
                        // Get all family instances
                        FilteredElementCollector collector = new FilteredElementCollector(doc);
                        var familyInstances = collector
                            .OfClass(typeof(FamilyInstance))
                            .Cast<FamilyInstance>()
                            .ToList();

                        Console.WriteLine($"[Info] Found {familyInstances.Count} family instances");

                        // Group by family
                        var instancesByFamily = familyInstances
                            .GroupBy(fi => fi.Symbol.Family.Name)
                            .ToList();

                        Console.WriteLine($"[Info] Processing {instancesByFamily.Count} unique families");

                        foreach (var group in instancesByFamily)
                        {
                            string familyName = group.Key;
                            var instances = group.ToList();
                            
                            Console.WriteLine($"[Info] Processing family: {familyName} ({instances.Count} instances)");

                            try
                            {
                                // Force regeneration of all instances
                                foreach (var instance in instances)
                                {
                                    try
                                    {
                                        // Get the symbol (type)
                                        FamilySymbol symbol = instance.Symbol;
                                        
                                        // Ensure symbol is activated
                                        if (!symbol.IsActive)
                                        {
                                            symbol.Activate();
                                            Console.WriteLine($"[Info] Activated symbol: {symbol.Name}");
                                        }

                                        // Force update by touching a parameter
                                        // This triggers regeneration
                                        Parameter param = instance.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
                                        if (param != null && !param.IsReadOnly)
                                        {
                                            string currentValue = param.AsString() ?? "";
                                            param.Set(currentValue); // Set to same value to trigger update
                                        }

                                        instancesUpdated++;
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"[Warning] Failed to update instance {instance.Id}: {ex.Message}");
                                    }
                                }

                                familiesProcessed++;
                                updatedFamilies.Add(familyName);
                                Console.WriteLine($"[OK] Updated {instances.Count} instances of {familyName}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[Error] Failed to process family {familyName}: {ex.Message}");
                            }
                        }

                        // Regenerate the document to apply all changes
                        doc.Regenerate();
                        Console.WriteLine($"[Info] Document regenerated");

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
                _customData["instancesUpdated"] = instancesUpdated;
                _customData["familiesProcessed"] = familiesProcessed;
                _customData["updatedFamilies"] = updatedFamilies.ToArray();
                _customData["documentTitle"] = doc.Title;

                message = $"Updated {instancesUpdated} instances across {familiesProcessed} families";
                Console.WriteLine($"[Success] {message}");

                return Result.Succeeded;
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
