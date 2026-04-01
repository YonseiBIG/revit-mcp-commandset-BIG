using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Common;
using RevitMCPSDK.API.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RevitMCPCommandSet.Services
{
    public class FindElementsByRoomEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        public List<ElementInfo> Result { get; private set; }
        public List<string> RoomNames { get; set; }
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

                var rooms = new FilteredElementCollector(doc)
                    .OfClass(typeof(SpatialElement))
                    .Cast<Room>()
                    .Where(r => r != null &&
                        RoomNames.Contains(r.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsValueString() ?? ""))
                    .ToList();

                if (rooms.Count == 0) return;

                var uniqueIds = new HashSet<ElementId>();

                if (Category.ToLower() == "wall")
                {
                    var walls = new FilteredElementCollector(doc)
                        .OfCategory(BuiltInCategory.OST_Walls)
                        .OfClass(typeof(Wall))
                        .Cast<Wall>()
                        .ToList();

                    foreach (var room in rooms)
                    {
                        var boundarySegments = room.GetBoundarySegments(new SpatialElementBoundaryOptions());
                        if (boundarySegments == null) continue;

                        foreach (var wall in walls)
                        {
                            if (uniqueIds.Contains(wall.Id)) continue;
                            var loc = wall.Location as LocationCurve;
                            if (loc == null) continue;

                            XYZ midPoint = loc.Curve.Evaluate(0.5, true);
                            BoundingBoxXYZ roomBB = room.get_BoundingBox(null);
                            if (roomBB == null) continue;

                            double z = roomBB.Min.Z + 0.1;
                            XYZ wallPoint = new XYZ(midPoint.X, midPoint.Y, z);

                            XYZ closestPoint = FindClosestPointOnBoundary(midPoint, boundarySegments);
                            if (closestPoint == null) continue;
                            XYZ direction = (closestPoint - midPoint).Normalize();
                            XYZ offsetPoint = wallPoint + direction * wall.Width;

                            if (room.IsPointInRoom(offsetPoint))
                            {
                                uniqueIds.Add(wall.Id);
                                AddElementInfo(wall);
                            }
                        }
                    }
                }
                else if (Category.ToLower() == "floor")
                {
                    var floors = new FilteredElementCollector(doc)
                        .OfCategory(BuiltInCategory.OST_Floors)
                        .OfClass(typeof(Floor))
                        .Cast<Floor>()
                        .ToList();

                    foreach (var room in rooms)
                    {
                        var boundarySegments = room.GetBoundarySegments(new SpatialElementBoundaryOptions());
                        if (boundarySegments == null) continue;

                        foreach (var floor in floors)
                        {
                            if (uniqueIds.Contains(floor.Id)) continue;
                            var loc = floor.Location as LocationCurve;
                            if (loc == null) continue;

                            XYZ midPoint = loc.Curve.Evaluate(0.5, true);
                            BoundingBoxXYZ roomBB = room.get_BoundingBox(null);
                            if (roomBB == null) continue;

                            double z = roomBB.Min.Z + 0.1;
                            XYZ floorPoint = new XYZ(midPoint.X, midPoint.Y, z);

                            XYZ closestPoint = FindClosestPointOnBoundary(midPoint, boundarySegments);
                            if (closestPoint == null) continue;
                            XYZ direction = (closestPoint - midPoint).Normalize();
                            double thickness = floor.get_Parameter(BuiltInParameter.FLOOR_ATTR_DEFAULT_THICKNESS_PARAM)?.AsDouble() ?? 0;
                            XYZ offsetPoint = floorPoint + direction * thickness;

                            if (room.IsPointInRoom(offsetPoint))
                            {
                                uniqueIds.Add(floor.Id);
                                AddElementInfo(floor);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", "Find elements by room failed: " + ex.Message);
                Result = new List<ElementInfo>();
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        private void AddElementInfo(Element elem)
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

        private static XYZ FindClosestPointOnBoundary(XYZ point, IList<IList<BoundarySegment>> boundarySegments)
        {
            XYZ closestPoint = null;
            double closestDistance = double.MaxValue;

            foreach (var segmentList in boundarySegments)
            {
                foreach (var segment in segmentList)
                {
                    Curve curve = segment.GetCurve();
                    IntersectionResult result = curve.Project(point);
                    XYZ projectedPoint = result.XYZPoint;
                    double distance = projectedPoint.DistanceTo(point);

                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestPoint = projectedPoint;
                    }
                }
            }

            return closestPoint;
        }

        public string GetName() => "FindElementsByRoom";
    }
}
