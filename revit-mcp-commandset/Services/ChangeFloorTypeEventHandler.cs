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
    public class ChangeFloorTypeEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        public AIResult<List<int>> Result { get; private set; }
        public List<int> ElementIds { get; set; }
        public string FloorTypeName { get; set; }

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
                var modifiedIds = new List<int>();

                FloorType targetType = new FilteredElementCollector(doc)
                    .OfClass(typeof(FloorType))
                    .Cast<FloorType>()
                    .FirstOrDefault(ft => ft.Name == FloorTypeName);

                if (targetType == null)
                {
                    Result = new AIResult<List<int>>
                    {
                        Success = false,
                        Message = $"FloorType '{FloorTypeName}' not found"
                    };
                    return;
                }

                using (Transaction tx = new Transaction(doc, "Change Floor Type"))
                {
                    tx.Start();
                    foreach (var id in ElementIds)
                    {
                        Floor floor = doc.GetElement(new ElementId(id)) as Floor;
                        if (floor != null)
                        {
                            floor.FloorType = targetType;
                            modifiedIds.Add(id);
                        }
                    }
                    tx.Commit();
                }

                Result = new AIResult<List<int>>
                {
                    Success = true,
                    Message = $"Changed {modifiedIds.Count} floor(s) to type '{FloorTypeName}'",
                    Response = modifiedIds
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<List<int>>
                {
                    Success = false,
                    Message = $"Change floor type failed: {ex.Message}"
                };
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public string GetName() => "ChangeFloorType";
    }
}
