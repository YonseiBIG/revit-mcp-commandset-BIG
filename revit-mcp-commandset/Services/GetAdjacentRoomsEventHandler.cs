using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RevitMCPCommandSet.Services
{
    public class GetAdjacentRoomsEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        public List<string> Result { get; private set; }
        public int TargetElementId { get; private set; }

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public void SetParameters(int targetElementId)
        {
            TargetElementId = targetElementId;
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
                Result = new List<string>();

                var element = doc.GetElement(new ElementId(TargetElementId));
                if (element == null) return;

                BoundingBoxXYZ bb = element.get_BoundingBox(null);
                if (bb == null) return;

                Outline outline = new Outline(bb.Min, bb.Max);
                ElementLevelFilter levelFilter = new ElementLevelFilter(element.LevelId);
                BoundingBoxIntersectsFilter bbFilter = new BoundingBoxIntersectsFilter(outline);

                var roomElems = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Rooms)
                    .WherePasses(bbFilter)
                    .WherePasses(levelFilter)
                    .ToElements();

                foreach (var elem in roomElems)
                {
                    Result.Add(elem.Name);
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", "Get adjacent rooms failed: " + ex.Message);
                Result = new List<string>();
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public string GetName() => "GetAdjacentRooms";
    }
}
