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
    public class GetElementsByTypeEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        public List<ElementInfo> Result { get; private set; }
        public string TypeName { get; set; }
        public string CategoryName { get; set; }

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

                BuiltInCategory bic = BuiltInCategory.INVALID;
                Enum.TryParse(CategoryName.Replace(".", ""), true, out bic);
                if (bic == BuiltInCategory.INVALID) return;

                var typeElement = new FilteredElementCollector(doc)
                    .OfCategory(bic)
                    .WhereElementIsElementType()
                    .FirstOrDefault(t => t.Name == TypeName);

                if (typeElement == null) return;

                var instances = new FilteredElementCollector(doc)
                    .OfCategory(bic)
                    .WhereElementIsNotElementType()
                    .Where(e => e.GetTypeId() == typeElement.Id)
                    .ToList();

                foreach (var elem in instances)
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
            catch (Exception ex)
            {
                TaskDialog.Show("Error", "Get elements by type failed: " + ex.Message);
                Result = new List<ElementInfo>();
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public string GetName() => "GetElementsByType";
    }
}
