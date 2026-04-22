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
    public class GetAvailableMaterialsEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        public AIResult<List<string>> Result { get; private set; }
        public string NameFilter { get; private set; }

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public void SetParameters(string nameFilter)
        {
            NameFilter = nameFilter;
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

                var query = new FilteredElementCollector(doc)
                    .OfClass(typeof(Material))
                    .Cast<Material>()
                    .Select(m => m.Name);

                if (!string.IsNullOrEmpty(NameFilter))
                {
                    string filter = NameFilter.ToLower();
                    query = query.Where(n => n.ToLower().Contains(filter));
                }

                var materials = query.OrderBy(n => n).ToList();

                Result = new AIResult<List<string>>
                {
                    Success = true,
                    Message = $"Found {materials.Count} materials",
                    Response = materials
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<List<string>>
                {
                    Success = false,
                    Message = $"Get available materials failed: {ex.Message}"
                };
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public string GetName() => "GetAvailableMaterials";
    }
}
