using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitCommandRunner.Core;

namespace SamplePlugin
{
    /// <summary>
    /// Sample command that demonstrates RevitCommandRunner usage.
    /// Creates walls and returns test results.
    /// </summary>
    public class CreateWallsCommand : IExternalCommandWithUIApp, ICommandWithData
    {
        private Dictionary<string, object> _customData = new Dictionary<string, object>();

        public Result Execute(UIApplication app, ref string message, ElementSet elements)
        {
            try
            {
                Console.WriteLine("[Info] Starting CreateWallsCommand");
                
                var doc = app.ActiveUIDocument.Document;
                
                if (doc == null)
                {
                    message = "No active document";
                    Console.WriteLine("[Error] No active document");
                    return Result.Failed;
                }

                Console.WriteLine($"[Info] Active document: {doc.Title}");

                int wallsCreated = 0;
                int wallsFailed = 0;

                using (Transaction trans = new Transaction(doc, "Create Sample Walls"))
                {
                    trans.Start();

                    try
                    {
                        // Get the first level
                        FilteredElementCollector levelCollector = new FilteredElementCollector(doc);
                        Level level = levelCollector.OfClass(typeof(Level)).FirstElement() as Level;

                        if (level == null)
                        {
                            message = "No levels found in document";
                            Console.WriteLine("[Error] No levels found");
                            return Result.Failed;
                        }

                        Console.WriteLine($"[Info] Using level: {level.Name}");

                        // Create 3 walls
                        for (int i = 0; i < 3; i++)
                        {
                            try
                            {
                                // Create line for wall
                                XYZ start = new XYZ(i * 10, 0, 0);
                                XYZ end = new XYZ(i * 10 + 10, 0, 0);
                                Line line = Line.CreateBound(start, end);

                                // Create wall
                                Wall wall = Wall.Create(doc, line, level.Id, false);
                                
                                wallsCreated++;
                                Console.WriteLine($"[OK] Created wall {i + 1} at X={i * 10}");
                            }
                            catch (Exception ex)
                            {
                                wallsFailed++;
                                Console.WriteLine($"[Error] Failed to create wall {i + 1}: {ex.Message}");
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
                _customData["wallsCreated"] = wallsCreated;
                _customData["wallsFailed"] = wallsFailed;
                _customData["documentTitle"] = doc.Title;

                message = $"Created {wallsCreated} walls, {wallsFailed} failed";
                Console.WriteLine($"[Success] {message}");

                return wallsFailed == 0 ? Result.Succeeded : Result.Failed;
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
