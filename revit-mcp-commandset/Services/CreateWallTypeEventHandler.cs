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
    /// Creates a new WallType with compound structure layers.
    /// Ported from NADIA_SK WallUtils.WallTypeFromJSON, 
    /// changed to accept materialName (string) instead of ElementId.
    /// </summary>
    public class CreateWallTypeEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        public AIResult<string> Result { get; private set; }
        public WallTypeCreationInfo CreationInfo { get; set; }

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

                string wallTypeName = CreationInfo.WallTypeName;
                int counter = 1;
                while (new FilteredElementCollector(doc).OfClass(typeof(WallType)).Any(wt => wt.Name == wallTypeName))
                {
                    wallTypeName = CreationInfo.WallTypeName + " " + counter;
                    counter++;
                }

                var resolvedLayers = new List<CompoundStructureLayer>();
                int exteriorCount = 0;
                bool foundStructure = false;

                foreach (var layerInfo in CreationInfo.Layers)
                {
                    Material material = new FilteredElementCollector(doc)
                        .OfClass(typeof(Material))
                        .FirstOrDefault(m => m.Name == layerInfo.MaterialName) as Material;

                    if (material == null)
                    {
                        Result = new AIResult<string>
                        {
                            Success = false,
                            Message = $"Material '{layerInfo.MaterialName}' not found in project"
                        };
                        return;
                    }

                    MaterialFunctionAssignment func =
                        (MaterialFunctionAssignment)Enum.Parse(typeof(MaterialFunctionAssignment), layerInfo.Function);

                    double widthFt = func == MaterialFunctionAssignment.Membrane
                        ? 0
                        : layerInfo.Thickness / 304.8;

                    resolvedLayers.Add(new CompoundStructureLayer()
                    {
                        MaterialId = material.Id,
                        Width = widthFt,
                        Function = func
                    });

                    if (func == MaterialFunctionAssignment.Structure)
                        foundStructure = true;

                    if (!foundStructure)
                        exteriorCount++;
                }

                using (Transaction tx = new Transaction(doc, "Create Wall Type"))
                {
                    tx.Start();

                    WallType baseWallType = new FilteredElementCollector(doc)
                        .OfClass(typeof(WallType))
                        .Cast<WallType>()
                        .FirstOrDefault(wt => wt.FamilyName == "Basic Wall")
                        ?? new FilteredElementCollector(doc)
                            .OfClass(typeof(WallType))
                            .Cast<WallType>()
                            .FirstOrDefault();

                    if (baseWallType == null)
                    {
                        tx.RollBack();
                        Result = new AIResult<string>
                        {
                            Success = false,
                            Message = "No base WallType found to duplicate"
                        };
                        return;
                    }

                    WallType newWallType = baseWallType.Duplicate(wallTypeName) as WallType;

                    CompoundStructure cs = CompoundStructure.CreateSimpleCompoundStructure(resolvedLayers);

                    if (foundStructure)
                    {
                        int interiorCount = resolvedLayers.Count - exteriorCount - 1;
                        cs.SetNumberOfShellLayers(ShellLayerType.Interior, interiorCount);
                        cs.SetNumberOfShellLayers(ShellLayerType.Exterior, exteriorCount);
                    }

                    newWallType.SetCompoundStructure(cs);
                    newWallType.get_Parameter(BuiltInParameter.WRAPPING_AT_ENDS_PARAM)?.Set(1);
                    newWallType.get_Parameter(BuiltInParameter.WRAPPING_AT_INSERTS_PARAM)?.Set(3);

                    tx.Commit();
                }

                Result = new AIResult<string>
                {
                    Success = true,
                    Message = $"Wall type '{wallTypeName}' created with {resolvedLayers.Count} layers",
                    Response = wallTypeName
                };
            }
            catch (Exception ex)
            {
                Result = new AIResult<string>
                {
                    Success = false,
                    Message = $"Create wall type failed: {ex.Message}"
                };
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public string GetName() => "CreateWallType";
    }
}
