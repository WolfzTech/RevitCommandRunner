using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitCommandRunner.Core;

namespace SamplePlugin
{
    /// <summary>
    /// Simple test command that doesn't modify the document.
    /// Just returns information about the active document.
    /// </summary>
    public class HelloRevitCommand : IExternalCommandWithUIApp, ICommandWithData
    {
        private Dictionary<string, object> _customData = new Dictionary<string, object>();

        public Result Execute(UIApplication app, ref string message, ElementSet elements)
        {
            try
            {
                Console.WriteLine("[Info] Starting HelloRevitCommand VERSION 3.0 - HOT RELOAD CONFIRMED!");
                
                var doc = app.ActiveUIDocument?.Document;
                
                if (doc == null)
                {
                    message = "No active document";
                    Console.WriteLine("[Warning] No active document");
                    
                    _customData["hasDocument"] = false;
                    _customData["revitVersion"] = app.Application.VersionNumber;
                    
                    return Result.Succeeded;
                }

                // Gather document info
                string docTitle = doc.Title;
                string docPath = doc.PathName;
                bool isModified = doc.IsModified;
                
                // Count elements
                FilteredElementCollector collector = new FilteredElementCollector(doc);
                int totalElements = collector.WhereElementIsNotElementType().GetElementCount();
                
                // Count walls
                FilteredElementCollector wallCollector = new FilteredElementCollector(doc);
                int wallCount = wallCollector.OfClass(typeof(Wall)).GetElementCount();
                
                // Count levels
                FilteredElementCollector levelCollector = new FilteredElementCollector(doc);
                int levelCount = levelCollector.OfClass(typeof(Level)).GetElementCount();

                Console.WriteLine($"[Info] Document: {docTitle}");
                Console.WriteLine($"[Info] Path: {(string.IsNullOrEmpty(docPath) ? "(not saved)" : docPath)}");
                Console.WriteLine($"[Info] Modified: {isModified}");
                Console.WriteLine($"[Info] Total elements: {totalElements}");
                Console.WriteLine($"[Info] Walls: {wallCount}");
                Console.WriteLine($"[Info] Levels: {levelCount}");

                // Store custom data
                _customData["hasDocument"] = true;
                _customData["documentTitle"] = docTitle;
                _customData["documentPath"] = docPath;
                _customData["isModified"] = isModified;
                _customData["totalElements"] = totalElements;
                _customData["wallCount"] = wallCount;
                _customData["levelCount"] = levelCount;
                _customData["revitVersion"] = app.Application.VersionNumber;

                message = $"🎉 HOT-RELOAD WORKS! Document: {docTitle}, Elements: {totalElements} [v3.0]";
                Console.WriteLine($"[Success] {message}");

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = $"Error: {ex.Message}";
                Console.WriteLine($"[Error] {ex.Message}");
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
