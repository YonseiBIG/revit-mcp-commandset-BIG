using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;

namespace RevitMCPCommandSet.Commands.Access
{
    public class GetElementsByTypeCommand : ExternalEventCommandBase
    {
        private GetElementsByTypeEventHandler _handler => (GetElementsByTypeEventHandler)Handler;

        public override string CommandName => "get_elements_by_type";

        public GetElementsByTypeCommand(UIApplication uiApp)
            : base(new GetElementsByTypeEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var typeName = parameters["typeName"]?.Value<string>();
                var category = parameters["category"]?.Value<string>();
                if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(category))
                    throw new ArgumentException("typeName and category are required");

                _handler.SetParameters(typeName, category);

                if (RaiseAndWaitForCompletion(15000))
                {
                    return _handler.Result;
                }
                else
                {
                    throw new TimeoutException("Get elements by type timed out");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Get elements by type failed: {ex.Message}");
            }
        }
    }
}
