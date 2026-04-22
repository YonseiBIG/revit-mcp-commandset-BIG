using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;

namespace RevitMCPCommandSet.Commands.Access
{
    public class GetAvailableMaterialsCommand : ExternalEventCommandBase
    {
        private GetAvailableMaterialsEventHandler _handler => (GetAvailableMaterialsEventHandler)Handler;

        public override string CommandName => "get_available_materials";

        public GetAvailableMaterialsCommand(UIApplication uiApp)
            : base(new GetAvailableMaterialsEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                _handler.SetParameters(parameters?["nameFilter"]?.Value<string>() ?? "");

                if (RaiseAndWaitForCompletion(15000))
                    return _handler.Result;
                else
                    throw new TimeoutException("Get available materials timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Get available materials failed: {ex.Message}");
            }
        }
    }
}
