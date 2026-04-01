using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;

namespace RevitMCPCommandSet.Commands.Access
{
    public class GetAdjacentRoomsCommand : ExternalEventCommandBase
    {
        private GetAdjacentRoomsEventHandler _handler => (GetAdjacentRoomsEventHandler)Handler;

        public override string CommandName => "get_adjacent_rooms";

        public GetAdjacentRoomsCommand(UIApplication uiApp)
            : base(new GetAdjacentRoomsEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var elementId = parameters["elementId"]?.Value<int>();
                if (elementId == null)
                    throw new ArgumentException("elementId is required");

                _handler.TargetElementId = elementId.Value;

                if (RaiseAndWaitForCompletion(15000))
                {
                    return _handler.Result;
                }
                else
                {
                    throw new TimeoutException("Get adjacent rooms timed out");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Get adjacent rooms failed: {ex.Message}");
            }
        }
    }
}
