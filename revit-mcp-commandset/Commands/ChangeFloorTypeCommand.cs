using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;
using System.Collections.Generic;

namespace RevitMCPCommandSet.Commands
{
    public class ChangeFloorTypeCommand : ExternalEventCommandBase
    {
        private ChangeFloorTypeEventHandler _handler => (ChangeFloorTypeEventHandler)Handler;

        public override string CommandName => "change_floor_type";

        public ChangeFloorTypeCommand(UIApplication uiApp)
            : base(new ChangeFloorTypeEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var elementIds = parameters["elementIds"]?.ToObject<List<int>>();
                var floorTypeName = parameters["floorTypeName"]?.Value<string>();
                if (elementIds == null || string.IsNullOrEmpty(floorTypeName))
                    throw new ArgumentException("elementIds and floorTypeName are required");

                _handler.ElementIds = elementIds;
                _handler.FloorTypeName = floorTypeName;

                if (RaiseAndWaitForCompletion(15000))
                    return _handler.Result;
                else
                    throw new TimeoutException("Change floor type timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Change floor type failed: {ex.Message}");
            }
        }
    }
}
