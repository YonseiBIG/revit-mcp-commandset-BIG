using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;

namespace RevitMCPCommandSet.Commands
{
    public class CreateWindowTypeCommand : ExternalEventCommandBase
    {
        private CreateOpeningTypeEventHandler _handler => (CreateOpeningTypeEventHandler)Handler;

        public override string CommandName => "create_window_type";

        public CreateWindowTypeCommand(UIApplication uiApp)
            : base(new CreateOpeningTypeEventHandler(Autodesk.Revit.DB.BuiltInCategory.OST_Windows), uiApp)
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

                _handler.FamilyName = familyName;
                _handler.Width = width.Value;
                _handler.Height = height.Value;

                if (RaiseAndWaitForCompletion(15000))
                    return _handler.Result;
                else
                    throw new TimeoutException("Create window type timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Create window type failed: {ex.Message}");
            }
        }
    }
}
