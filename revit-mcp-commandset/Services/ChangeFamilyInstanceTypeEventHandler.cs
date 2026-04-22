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
    /// <summary>
    /// Shared handler for changing FamilySymbol on FamilyInstance elements (beam, column, door).
    /// </summary>
    public class ChangeFamilyInstanceTypeEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        public AIResult<List<int>> Result { get; private set; }
        public List<int> ElementIds { get; private set; }
        public string TargetSymbolName { get; private set; }

        private readonly BuiltInCategory _category;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public ChangeFamilyInstanceTypeEventHandler(BuiltInCategory category)
        {
            _category = category;
        }

        public void SetParameters(List<int> elementIds, string targetSymbolName)
        {
            ElementIds = elementIds;
            TargetSymbolName = targetSymbolName;
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

                FamilySymbol targetSymbol = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .OfCategory(_category)
                    .Cast<FamilySymbol>()
                    .FirstOrDefault(fs => fs.Name == TargetSymbolName);

                if (targetSymbol == null)
                {
                    Result = new AIResult<List<int>>
                    {
                        Success = false,
                        Message = $"FamilySymbol '{TargetSymbolName}' not found in category {_category}"
                    };
                    return;
                }

                using (Transaction tx = new Transaction(doc, "Change Element Type"))
                {
                    tx.Start();
                    if (!targetSymbol.IsActive)
                        targetSymbol.Activate();

                    foreach (var id in ElementIds)
                    {
                        FamilyInstance fi = doc.GetElement(new ElementId(id)) as FamilyInstance;
                        if (fi != null)
                        {
                            fi.Symbol = targetSymbol;
                            modifiedIds.Add(id);
                        }
                    }
                    tx.Commit();
                }

                Result = new AIResult<List<int>>
                {
                    Success = true,
                    Message = $"Changed {modifiedIds.Count} element(s) to '{TargetSymbolName}'",
                    Response = modifiedIds
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<List<int>>
                {
                    Success = false,
                    Message = $"Change type failed: {ex.Message}"
                };
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public string GetName() => $"ChangeFamilyInstanceType_{_category}";
    }
}
