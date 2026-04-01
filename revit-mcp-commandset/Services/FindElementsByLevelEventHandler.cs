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
    public class FindElementsByLevelEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        public List<ElementInfo> Result { get; private set; }
        public List<string> LevelNames { get; set; }
        public string Category { get; set; }

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

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

                var levels = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .Where(l => LevelNames.Contains(
                        l.get_Parameter(BuiltInParameter.DATUM_TEXT)?.AsValueString() ?? l.Name))
                    .ToList();

                if (levels.Count == 0) return;

                var levelIds = new HashSet<ElementId>(levels.Select(l => l.Id));

                BuiltInCategory bic;
                switch (Category.ToLower())
                {
                    case "wall":
                        bic = BuiltInCategory.OST_Walls;
                        break;
                    case "beam":
                        bic = BuiltInCategory.OST_StructuralFraming;
                        break;
                    case "column":
                        bic = BuiltInCategory.OST_StructuralColumns;
                        break;
                    case "floor":
                        bic = BuiltInCategory.OST_Floors;
                        break;
                    default:
                        return;
                }

                var elements = new FilteredElementCollector(doc)
                    .OfCategory(bic)
                    .WhereElementIsNotElementType()
                    .ToElements();

                foreach (var elem in elements)
                {
                    ElementId elemLevelId = null;

                    if (elem is Wall wall)
                        elemLevelId = wall.LevelId;
                    else if (elem is Floor floor)
                        elemLevelId = floor.LevelId;
                    else if (elem is FamilyInstance fi)
                    {
                        var refLevelParam = fi.get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM);
                        elemLevelId = refLevelParam?.AsElementId() ?? fi.LevelId;
                    }

                    if (elemLevelId != null && levelIds.Contains(elemLevelId))
                    {
                        Result.Add(new ElementInfo
                        {
#if REVIT2024_OR_GREATER
                            Id = elem.Id.Value,
#else
                            Id = elem.Id.IntegerValue,
#endif
                            UniqueId = elem.UniqueId,
                            Name = elem.Name,
                            Category = elem.Category?.Name
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", "Find elements by level failed: " + ex.Message);
                Result = new List<ElementInfo>();
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public string GetName() => "FindElementsByLevel";
    }
}
