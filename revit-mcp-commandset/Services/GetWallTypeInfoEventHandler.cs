using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Interfaces;
using System;
using System.Linq;
using System.Threading;

namespace RevitMCPCommandSet.Services
{
    /// <summary>
    /// Returns wall type compound structure as JSON.
    /// Ported from NADIA_SK WallUtils.WallTypeToJSON.
    /// </summary>
    public class GetWallTypeInfoEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        public JObject Result { get; private set; }
        public string WallTypeName { get; set; }

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

                WallType wallType = new FilteredElementCollector(doc)
                    .OfClass(typeof(WallType))
                    .Cast<WallType>()
                    .FirstOrDefault(wt => wt.Name == WallTypeName);

                if (wallType == null)
                {
                    Result = new JObject
                    {
                        ["success"] = false,
                        ["message"] = $"WallType '{WallTypeName}' not found"
                    };
                    return;
                }

                var typeObject = new JObject();
                typeObject["wall_detail_name"] = wallType.Name;

                CompoundStructure cs = wallType.GetCompoundStructure();
                var layersArray = new JArray();

                if (cs != null)
                {
                    for (int i = 0; i < cs.LayerCount; i++)
                    {
                        var layerObj = new JObject();

                        ElementId materialId = cs.GetMaterialId(i);
                        Material material = doc.GetElement(materialId) as Material;
                        layerObj["material"] = material?.Name ?? "None";
                        layerObj["thickness"] = Math.Round(cs.GetLayerWidth(i) * 304.8, 1);
                        layerObj["layer_type"] = cs.GetLayerFunction(i).ToString();

                        layersArray.Add(layerObj);
                    }
                }

                typeObject["layers"] = layersArray;
                typeObject["success"] = true;
                Result = typeObject;
            }
            catch (Exception ex)
            {
                Result = new JObject
                {
                    ["success"] = false,
                    ["message"] = $"Get wall type info failed: {ex.Message}"
                };
            }
            finally
            {
                _resetEvent.Set();
            }
        }

        public string GetName() => "GetWallTypeInfo";
    }
}
