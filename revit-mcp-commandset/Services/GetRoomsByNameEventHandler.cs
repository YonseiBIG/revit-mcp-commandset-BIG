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
    public class GetRoomsByNameEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        public List<ElementInfo> Result { get; private set; }
        public List<string> RoomNames { get; set; }

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

                var roomCollection = new FilteredElementCollector(doc)
                    .OfClass(typeof(SpatialElement))
                    .ToElements();

                foreach (Element roomElement in roomCollection)
                {
                    if (roomElement is Room room)
                    {
                        string roomName = room.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsValueString();
                        if (roomName != null && RoomNames.Contains(roomName))
                        {
                            Result.Add(new ElementInfo
                            {
#if REVIT2024_OR_GREATER
                                Id = room.Id.Value,
#else
                                Id = room.Id.IntegerValue,
#endif
                                UniqueId = room.UniqueId,
                                Name = roomName,
                                Category = room.Category?.Name,
                                Properties = new Dictionary<string, string>
                                {
                                    ["Area"] = room.get_Parameter(BuiltInParameter.ROOM_AREA)?.AsValueString() ?? "",
                                    ["Level"] = room.Level?.Name ?? "",
                                    ["Number"] = room.Number ?? ""
                                }
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", "Get rooms by name failed: " + ex.Message);
                Result = new List<ElementInfo>();
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public string GetName() => "GetRoomsByName";
    }
}
