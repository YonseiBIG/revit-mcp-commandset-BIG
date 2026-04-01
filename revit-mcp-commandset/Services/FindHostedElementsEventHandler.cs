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
    public class FindHostedElementsEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        public List<ElementInfo> Result { get; private set; }
        public List<int> HostElementIds { get; set; }
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

                var hostIds = new HashSet<ElementId>(
                    HostElementIds.Select(id => new ElementId(id)));

                BuiltInCategory bic = Category.ToLower() == "door"
                    ? BuiltInCategory.OST_Doors
                    : BuiltInCategory.OST_Windows;

                var instances = new FilteredElementCollector(doc)
                    .OfCategory(bic)
                    .OfClass(typeof(FamilyInstance))
                    .Cast<FamilyInstance>()
                    .ToList();

                var uniqueIds = new HashSet<ElementId>();

                foreach (var fi in instances)
                {
                    if (fi.Host != null && hostIds.Contains(fi.Host.Id) && !uniqueIds.Contains(fi.Id))
                    {
                        uniqueIds.Add(fi.Id);
                        Result.Add(new ElementInfo
                        {
#if REVIT2024_OR_GREATER
                            Id = fi.Id.Value,
#else
                            Id = fi.Id.IntegerValue,
#endif
                            UniqueId = fi.UniqueId,
                            Name = fi.Name,
                            Category = fi.Category?.Name,
                            Properties = new Dictionary<string, string>
                            {
#if REVIT2024_OR_GREATER
                                ["HostId"] = fi.Host.Id.Value.ToString(),
#else
                                ["HostId"] = fi.Host.Id.IntegerValue.ToString(),
#endif
                                ["HostName"] = fi.Host.Name
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", "Find hosted elements failed: " + ex.Message);
                Result = new List<ElementInfo>();
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public string GetName() => "FindHostedElements";
    }
}
