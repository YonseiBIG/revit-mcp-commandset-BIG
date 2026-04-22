using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Common;
using RevitMCPSDK.API.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RevitMCPCommandSet.Services
{
    public class GetLevelsByNameEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        public List<ElementInfo> Result { get; private set; }
        public List<string> LevelNames { get; private set; }

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public void SetParameters(List<string> levelNames)
        {
            LevelNames = levelNames;
            _resetEvent.Reset();
        }

        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        public void Execute(UIApplication app)
        {
            try
            {
                var doc = app.ActiveUIDocument.Document;
                Result = new List<ElementInfo>();

                var levelCollection = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .ToElements();

                foreach (Element levelElement in levelCollection)
                {
                    if (levelElement is Level level)
                    {
                        string levelName = level.get_Parameter(BuiltInParameter.DATUM_TEXT)?.AsValueString();
                        if (levelName != null && LevelNames.Contains(levelName))
                        {
                            Result.Add(new ElementInfo
                            {
#if REVIT2024_OR_GREATER
                                Id = level.Id.Value,
#else
                                Id = level.Id.IntegerValue,
#endif
                                UniqueId = level.UniqueId,
                                Name = levelName,
                                Category = level.Category?.Name,
                                Properties = new Dictionary<string, string>
                                {
                                    ["Elevation"] = (level.Elevation * 304.8).ToString("F1")
                                }
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", "Get levels by name failed: " + ex.Message);
                Result = new List<ElementInfo>();
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public string GetName() => "GetLevelsByName";
    }
}
