using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;
using System.Collections.Generic;

namespace RevitMCPCommandSet.Commands.Access
{
    public class GetRoomsByNameCommand : ExternalEventCommandBase
    {
        private GetRoomsByNameEventHandler _handler => (GetRoomsByNameEventHandler)Handler;

        public override string CommandName => "get_rooms_by_name";

        public GetRoomsByNameCommand(UIApplication uiApp)
            : base(new GetRoomsByNameEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var roomNames = parameters["roomNames"]?.ToObject<List<string>>();
                if (roomNames == null || roomNames.Count == 0)
                    throw new ArgumentException("roomNames is required");

                _handler.RoomNames = roomNames;

                if (RaiseAndWaitForCompletion(15000))
                {
                    return _handler.Result;
                }
                else
                {
                    throw new TimeoutException("Get rooms by name timed out");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Get rooms by name failed: {ex.Message}");
            }
        }
    }
}
