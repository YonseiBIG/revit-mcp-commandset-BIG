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
    /// Shared handler for creating door/window types by duplicating and setting dimensions.
    /// Ported from NADIA_SK DoorUtils.AddDoorType / WindowUtils.AddWindowType.
    /// </summary>
    public class CreateOpeningTypeEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        public AIResult<string> Result { get; private set; }
        public string FamilyName { get; private set; }
        public double Width { get; private set; }
        public double Height { get; private set; }

        private readonly BuiltInCategory _category;
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public CreateOpeningTypeEventHandler(BuiltInCategory category)
        {
            _category = category;
        }

        public void SetParameters(string familyName, double width, double height)
        {
            FamilyName = familyName;
            Width = width;
            Height = height;
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

                Family family = new FilteredElementCollector(doc)
                    .OfClass(typeof(Family))
                    .Cast<Family>()
                    .FirstOrDefault(f => f.Name == FamilyName &&
                        f.FamilyCategoryId == new ElementId(_category));

                if (family == null)
                {
                    FamilySymbol symbol = new FilteredElementCollector(doc)
                        .OfClass(typeof(FamilySymbol))
                        .OfCategory(_category)
                        .Cast<FamilySymbol>()
                        .FirstOrDefault(fs => fs.FamilyName == FamilyName);

                    if (symbol != null)
                        family = symbol.Family;
                }

                if (family == null)
                {
                    Result = new AIResult<string>
                    {
                        Success = false,
                        Message = $"Family '{FamilyName}' not found in category {_category}"
                    };
                    return;
                }

                int intW = (int)Math.Round(Width);
                int intH = (int)Math.Round(Height);
                string newName = $"{intW}x{intH}";

                using (Transaction tx = new Transaction(doc, "Create Opening Type"))
                {
                    tx.Start();

                    List<ElementId> symbolIds = family.GetFamilySymbolIds().ToList();

                    foreach (ElementId eid in symbolIds)
                    {
                        Element elem = doc.GetElement(eid);
                        if (elem.Name == newName)
                        {
                            doc.Delete(eid);
                            break;
                        }
                    }

                    symbolIds = family.GetFamilySymbolIds().ToList();
                    FamilySymbol baseSymbol = doc.GetElement(symbolIds.First()) as FamilySymbol;
                    ElementType dupSymbol = baseSymbol.Duplicate(newName);

                    dupSymbol.get_Parameter(BuiltInParameter.FURNITURE_WIDTH)?.Set(intW / 304.8);
                    dupSymbol.get_Parameter(BuiltInParameter.FAMILY_HEIGHT_PARAM)?.Set(intH / 304.8);

                    tx.Commit();

                    Result = new AIResult<string>
                    {
                        Success = true,
                        Message = $"Created type '{newName}' in family '{FamilyName}'",
                        Response = newName
                    };
                }
            }
            catch (Exception ex)
            {
                Result = new AIResult<string>
                {
                    Success = false,
                    Message = $"Create opening type failed: {ex.Message}"
                };
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public string GetName() => $"CreateOpeningType_{_category}";
    }
}
