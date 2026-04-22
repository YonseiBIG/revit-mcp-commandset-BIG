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
    public class ChangeWallTypeEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        public AIResult<List<int>> Result { get; private set; }
        public List<int> ElementIds { get; private set; }
        public string WallTypeName { get; private set; }

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public void SetParameters(List<int> elementIds, string wallTypeName)
        {
            ElementIds = elementIds;
            WallTypeName = wallTypeName;
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
                var modifiedIds = new List<int>();

                WallType targetType = new FilteredElementCollector(doc)
                    .OfClass(typeof(WallType))
                    .Cast<WallType>()
                    .FirstOrDefault(wt => wt.Name == WallTypeName);

                if (targetType == null)
                {
                    Result = new AIResult<List<int>>
                    {
                        Success = false,
                        Message = $"WallType '{WallTypeName}' not found"
                    };
                    return;
                }

                using (Transaction tx = new Transaction(doc, "Change Wall Type"))
                {
                    tx.Start();
                    foreach (var id in ElementIds)
                    {
                        Wall wall = doc.GetElement(new ElementId(id)) as Wall;
                        if (wall != null)
                        {
                            wall.WallType = targetType;
                            modifiedIds.Add(id);
                        }
                    }
                    tx.Commit();
                }

                Result = new AIResult<List<int>>
                {
                    Success = true,
                    Message = $"Changed {modifiedIds.Count} wall(s) to type '{WallTypeName}'",
                    Response = modifiedIds
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<List<int>>
                {
                    Success = false,
                    Message = $"Change wall type failed: {ex.Message}"
                };
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public string GetName() => "ChangeWallType";
    }
}
