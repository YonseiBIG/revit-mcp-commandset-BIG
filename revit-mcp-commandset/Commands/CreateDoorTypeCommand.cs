using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;

namespace RevitMCPCommandSet.Commands
{
    public class CreateDoorTypeCommand : ExternalEventCommandBase
    {
        private CreateOpeningTypeEventHandler _handler => (CreateOpeningTypeEventHandler)Handler;

        public override string CommandName => "create_door_type";

        public CreateDoorTypeCommand(UIApplication uiApp)
            : base(new CreateOpeningTypeEventHandler(Autodesk.Revit.DB.BuiltInCategory.OST_Doors), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var familyName = parameters["familyName"]?.Value<string>();
                var width = parameters["width"]?.Value<double>();
                var height = parameters["height"]?.Value<double>();
                if (string.IsNullOrEmpty(familyName) || width == null || height == null)
                    throw new ArgumentException("familyName, width, and height are required");

                _handler.SetParameters(familyName, width.Value, height.Value);

                if (RaiseAndWaitForCompletion(15000))
                    return _handler.Result;
                else
                    throw new TimeoutException("Create door type timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Create door type failed: {ex.Message}");
            }
        }
    }
}
