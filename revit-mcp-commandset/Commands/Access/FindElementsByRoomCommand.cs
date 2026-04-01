using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;
using System.Collections.Generic;

namespace RevitMCPCommandSet.Commands.Access
{
    public class FindElementsByRoomCommand : ExternalEventCommandBase
    {
        private FindElementsByRoomEventHandler _handler => (FindElementsByRoomEventHandler)Handler;

        public override string CommandName => "find_elements_by_room";

        public FindElementsByRoomCommand(UIApplication uiApp)
            : base(new FindElementsByRoomEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var roomNames = parameters["roomNames"]?.ToObject<List<string>>();
                var category = parameters["category"]?.Value<string>();
                if (roomNames == null || string.IsNullOrEmpty(category))
                    throw new ArgumentException("roomNames and category are required");

                _handler.RoomNames = roomNames;
                _handler.Category = category;

                if (RaiseAndWaitForCompletion(30000))
                {
                    return _handler.Result;
                }
                else
                {
                    throw new TimeoutException("Find elements by room timed out");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Find elements by room failed: {ex.Message}");
            }
        }
    }
}
