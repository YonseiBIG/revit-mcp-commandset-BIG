using Autodesk.Revit.UI;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Utils;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Services
{
    public class CreateSurfaceElementEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;
        private Autodesk.Revit.ApplicationServices.Application app => uiApp.Application;
        /// <summary>
        /// Event wait object
        /// </summary>
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);
        /// <summary>
        /// Data for creation (input)
        /// </summary>
        public List<SurfaceElement> CreatedInfo { get; private set; }
        /// <summary>
        /// Execution result (output)
        /// </summary>
        public AIResult<List<int>> Result { get; private set; }
        public string _floorName = "Generic - ";
        public bool _structural = true;

        /// <summary>
        /// Set the creation parameters
        /// </summary>
        public void SetParameters(List<SurfaceElement> data)
        {
            CreatedInfo = data;
            _resetEvent.Reset();
        }
        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;

            try
            {
                var elementIds = new List<int>();
                foreach (var data in CreatedInfo)
                {
                    // Step 0: get the element category
                    BuiltInCategory builtInCategory = BuiltInCategory.INVALID;
                    Enum.TryParse(data.Category.Replace(".", "").Replace("BuiltInCategory", ""), true, out builtInCategory);

                    // Step 1: get level and offset
                    Level baseLevel = null;
                    Level topLevel = null;
                    double topOffset = -1;  // ft
                    double baseOffset = -1; // ft
                    baseLevel = doc.FindNearestLevel(data.BaseLevel / 304.8);
                    baseOffset = (data.BaseOffset + data.BaseLevel) / 304.8 - baseLevel.Elevation;
                    topLevel = doc.FindNearestLevel((data.BaseLevel + data.BaseOffset + data.Thickness) / 304.8);
                    topOffset = (data.BaseLevel + data.BaseOffset + data.Thickness) / 304.8 - topLevel.Elevation;
                    if (baseLevel == null)
                        continue;

                    // Step 2: get the family type
                    FamilySymbol symbol = null;
                    FloorType floorType = null;
                    if (data.TypeId != -1 && data.TypeId != 0)
                    {
                        ElementId typeELeId = new ElementId(data.TypeId);
                        if (typeELeId != null)
                        {
                            Element typeEle = doc.GetElement(typeELeId);
                            if (typeEle != null && typeEle is FamilySymbol)
                            {
                                symbol = typeEle as FamilySymbol;
                                // Get the Category object of the symbol and convert it to a BuiltInCategory enum
                                builtInCategory = (BuiltInCategory)symbol.Category.Id.IntegerValue;
                            }
                            else if (typeEle != null && typeEle is FloorType)
                            {
                                floorType = typeEle as FloorType;
                                builtInCategory = (BuiltInCategory)floorType.Category.Id.IntegerValue;
                            }
                        }
                    }
                    if (builtInCategory == BuiltInCategory.INVALID)
                        continue;
                    switch (builtInCategory)
                    {
                        case BuiltInCategory.OST_Floors:
                            if (floorType == null)
                            {
                                using (Transaction transaction = new Transaction(doc, "Create Floor Type"))
                                {
                                    transaction.Start();
                                    floorType = CreateOrGetFloorType(doc, data.Thickness / 304.8);
                                    transaction.Commit();
                                }
                                if (floorType == null)
                                    continue;
                            }
                            break;
                        default:
                            if (symbol == null)
                            {
                                symbol = new FilteredElementCollector(doc)
                                    .OfClass(typeof(FamilySymbol))
                                    .OfCategory(builtInCategory)
                                    .Cast<FamilySymbol>()
                                    .FirstOrDefault(fs => fs.IsActive); // Use an active type as the default
                                if (symbol == null)
                                {
                                    symbol = new FilteredElementCollector(doc)
                                    .OfClass(typeof(FamilySymbol))
                                    .OfCategory(builtInCategory)
                                    .Cast<FamilySymbol>()
                                    .FirstOrDefault();
                                }
                            }
                            if (symbol == null)
                                continue;
                            break;
                    }

                    // Step 3: bulk-create floors
                    Floor floor = null;
                    using (Transaction transaction = new Transaction(doc, "Create Surface-Based Element"))
                    {
                        transaction.Start();

                        switch (builtInCategory)
                        {
                            case BuiltInCategory.OST_Floors:
                                CurveArray curves = new CurveArray();
                                foreach (var jzLine in data.Boundary.OuterLoop)
                                {
                                    curves.Append(JZLine.ToLine(jzLine));
                                }
                                CurveLoop curveLoop = CurveLoop.Create(data.Boundary.OuterLoop.Select(l => JZLine.ToLine(l) as Curve).ToList());

                                // Multi-version support
#if REVIT2022_OR_GREATER
                                floor = Floor.Create(doc, new List<CurveLoop> { curveLoop }, floorType.Id, baseLevel.Id);
#else
                                floor = doc.Create.NewFloor(curves, floorType, baseLevel, _structural);
#endif
                                // Edit floor parameters
                                if (floor != null)
                                {
                                    floor.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM).Set(baseOffset);
                                    elementIds.Add(floor.Id.IntegerValue);
                                }
                                break;
                            default:

                                break;
                        }

                        transaction.Commit();
                    }
                }
                Result = new AIResult<List<int>>
                {
                    Success = true,
                    Message = $"Successfully created {elementIds.Count} family instance(s); the ElementIds are stored in the Response property",
                    Response = elementIds,
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<List<int>>
                {
                    Success = false,
                    Message = $"Error while creating surface-based elements: {ex.Message}",
                };
                TaskDialog.Show("Error", $"Error while creating surface-based elements: {ex.Message}");
            }
            finally
            {
                _resetEvent.Set(); // Signal the waiting thread that the operation is complete
            }
        }

        /// <summary>
        /// Wait for creation to complete
        /// </summary>
        /// <param name="timeoutMilliseconds">Timeout in milliseconds</param>
        /// <returns>Whether the operation completed before the timeout</returns>
        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        /// <summary>
        /// IExternalEventHandler.GetName implementation
        /// </summary>
        public string GetName()
        {
            return "Create Surface-Based Element";
        }

        /// <summary>
        /// Retrieve or create a floor type with the specified thickness
        /// </summary>
        /// <param name="thickness">Target thickness (ft)</param>
        /// <returns>A floor type matching the requested thickness</returns>
        private FloorType CreateOrGetFloorType(Document doc, double thickness = 200 / 304.8)
        {

            // Find a floor type matching the thickness
            FloorType existingType = new FilteredElementCollector(doc)
                                     .OfClass(typeof(FloorType))                    // Only FloorType classes
                                     .OfCategory(BuiltInCategory.OST_Floors)        // Only the Floors category
                                     .Cast<FloorType>()                            // Cast to FloorType
                                     .FirstOrDefault(w => w.Name == $"{_floorName}{thickness * 304.8}mm");
            if (existingType != null)
                return existingType;
            // If no matching floor type was found, create a new one
            FloorType baseFloorType = existingType = new FilteredElementCollector(doc)
                                     .OfClass(typeof(FloorType))                    // Only FloorType classes
                                     .OfCategory(BuiltInCategory.OST_Floors)        // Only the Floors category
                                     .Cast<FloorType>()                            // Cast to FloorType
                                     .FirstOrDefault(w => w.Name.Contains("Generic"));
            if (existingType != null)
            {
                baseFloorType = existingType = new FilteredElementCollector(doc)
                                     .OfClass(typeof(FloorType))                    // Only FloorType classes
                                     .OfCategory(BuiltInCategory.OST_Floors)        // Only the Floors category
                                     .Cast<FloorType>()                            // Cast to FloorType
                                     .FirstOrDefault();
            }

            // Duplicate the floor type
            FloorType newFloorType = null;
            newFloorType = baseFloorType.Duplicate($"{_floorName}{thickness * 304.8}mm") as FloorType;

            // Set the thickness of the new floor type
            // Get the structure layer settings
            CompoundStructure cs = newFloorType.GetCompoundStructure();
            if (cs != null)
            {
                // Get all layers
                IList<CompoundStructureLayer> layers = cs.GetLayers();
                if (layers.Count > 0)
                {
                    // Calculate the current total thickness
                    double currentTotalThickness = cs.GetWidth();

                    // Adjust each layer thickness proportionally
                    for (int i = 0; i < layers.Count; i++)
                    {
                        CompoundStructureLayer layer = layers[i];
                        double newLayerThickness = thickness;
                        cs.SetLayerWidth(i, newLayerThickness);
                    }

                    // Apply the modified structure layer settings
                    newFloorType.SetCompoundStructure(cs);
                }
            }
            return newFloorType;
        }

    }
}
