using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;
using System.Collections.Generic;

namespace RevitMCPCommandSet.Commands
{
    public class ChangeWallTypeCommand : ExternalEventCommandBase
    {
        private ChangeWallTypeEventHandler _handler => (ChangeWallTypeEventHandler)Handler;

        public override string CommandName => "change_wall_type";

        public ChangeWallTypeCommand(UIApplication uiApp)
            : base(new ChangeWallTypeEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var elementIds = parameters["elementIds"]?.ToObject<List<int>>();
                var wallTypeName = parameters["wallTypeName"]?.Value<string>();
                if (elementIds == null || string.IsNullOrEmpty(wallTypeName))
                    throw new ArgumentException("elementIds and wallTypeName are required");

                _handler.ElementIds = elementIds;
                _handler.WallTypeName = wallTypeName;

                if (RaiseAndWaitForCompletion(15000))
                {
                    return _handler.Result;
                }
                else
                {
                    throw new TimeoutException("Change wall type timed out");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Change wall type failed: {ex.Message}");
            }
        }
    }
}
