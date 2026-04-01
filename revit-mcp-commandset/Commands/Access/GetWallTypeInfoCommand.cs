using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;

namespace RevitMCPCommandSet.Commands.Access
{
    public class GetWallTypeInfoCommand : ExternalEventCommandBase
    {
        private GetWallTypeInfoEventHandler _handler => (GetWallTypeInfoEventHandler)Handler;

        public override string CommandName => "get_wall_type_info";

        public GetWallTypeInfoCommand(UIApplication uiApp)
            : base(new GetWallTypeInfoEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var wallTypeName = parameters["wallTypeName"]?.Value<string>();
                if (string.IsNullOrEmpty(wallTypeName))
                    throw new ArgumentException("wallTypeName is required");

                _handler.WallTypeName = wallTypeName;

                if (RaiseAndWaitForCompletion(15000))
                    return _handler.Result;
                else
                    throw new TimeoutException("Get wall type info timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Get wall type info failed: {ex.Message}");
            }
        }
    }
}
