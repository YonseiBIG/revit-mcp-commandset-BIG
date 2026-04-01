using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Common;
using RevitMCPSDK.API.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace RevitMCPCommandSet.Services
{
    /// <summary>
    /// Changes window FamilySymbol while preserving original dimensions.
    /// Creates matching size types in the target family if they don't exist.
    /// Ported from NADIA_SK WindowUtils.WindowGeneration + AddWindowType.
    /// </summary>
    public class ChangeWindowTypeEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        public AIResult<List<int>> Result { get; private set; }
        public List<int> ElementIds { get; set; }
        public string TargetFamilyName { get; set; }

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

                FamilySymbol targetFamilySymbol = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .Cast<FamilySymbol>()
                    .FirstOrDefault(fs => fs.FamilyName == TargetFamilyName);

                if (targetFamilySymbol == null)
                {
                    Result = new AIResult<List<int>>
                    {
                        Success = false,
                        Message = $"Window family '{TargetFamilyName}' not found"
                    };
                    return;
                }

                var createdSizes = new HashSet<string>();

                using (Transaction tx = new Transaction(doc, "Change Window Type"))
                {
                    tx.Start();

                    foreach (var id in ElementIds)
                    {
                        FamilyInstance window = doc.GetElement(new ElementId(id)) as FamilyInstance;
                        if (window == null) continue;

                        Parameter widthParam = window.Symbol.get_Parameter(BuiltInParameter.FURNITURE_WIDTH);
                        Parameter heightParam = window.Symbol.get_Parameter(BuiltInParameter.FAMILY_HEIGHT_PARAM);
                        if (widthParam == null || heightParam == null) continue;

                        string widthStr = widthParam.AsValueString();
                        string heightStr = heightParam.AsValueString();
                        string sizeKey = $"{widthStr}x{heightStr}";

                        if (!createdSizes.Contains(sizeKey))
                        {
                            FamilySymbol newSymbol = CreateOrGetWindowType(
                                doc, targetFamilySymbol.Family, widthStr, heightStr);
                            window.Symbol = newSymbol;
                            createdSizes.Add(sizeKey);
                        }
                        else
                        {
                            string sizeName = $"{TargetFamilyName} {widthStr}x{heightStr}";
                            FamilySymbol existingSymbol = new FilteredElementCollector(doc)
                                .OfClass(typeof(FamilySymbol))
                                .Cast<FamilySymbol>()
                                .FirstOrDefault(s => s.FamilyName == TargetFamilyName && s.Name == sizeName);

                            if (existingSymbol != null)
                            {
                                if (!existingSymbol.IsActive) existingSymbol.Activate();
                                window.Symbol = existingSymbol;
                            }
                        }

                        modifiedIds.Add(id);
                    }

                    tx.Commit();
                }

                Result = new AIResult<List<int>>
                {
                    Success = true,
                    Message = $"Changed {modifiedIds.Count} window(s) to family '{TargetFamilyName}'",
                    Response = modifiedIds
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<List<int>>
                {
                    Success = false,
                    Message = $"Change window type failed: {ex.Message}"
                };
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        private FamilySymbol CreateOrGetWindowType(Document doc, Family family, string width, string height)
        {
            int w = int.Parse(width);
            int h = int.Parse(height);
            string newName = $"{family.Name} {w}x{h}";

            List<ElementId> symbols = family.GetFamilySymbolIds().ToList();

            foreach (ElementId eid in symbols)
            {
                Element elem = doc.GetElement(eid);
                if (elem.Name == newName)
                {
                    doc.Delete(eid);
                    break;
                }
            }

            ElementType dupSymbol = (doc.GetElement(symbols.First()) as FamilySymbol).Duplicate(newName);
            dupSymbol.get_Parameter(BuiltInParameter.FURNITURE_WIDTH).Set(w / 304.8);
            dupSymbol.get_Parameter(BuiltInParameter.FAMILY_HEIGHT_PARAM).Set(h / 304.8);

            return dupSymbol as FamilySymbol;
        }

        public string GetName() => "ChangeWindowType";
    }
}
