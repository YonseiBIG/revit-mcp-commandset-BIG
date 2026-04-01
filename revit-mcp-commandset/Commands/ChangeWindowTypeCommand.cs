using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;
using System.Collections.Generic;

namespace RevitMCPCommandSet.Commands
{
    public class ChangeWindowTypeCommand : ExternalEventCommandBase
    {
        private ChangeWindowTypeEventHandler _handler => (ChangeWindowTypeEventHandler)Handler;

        public override string CommandName => "change_window_type";

        public ChangeWindowTypeCommand(UIApplication uiApp)
            : base(new ChangeWindowTypeEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var elementIds = parameters["elementIds"]?.ToObject<List<int>>();
                var familyName = parameters["familyName"]?.Value<string>();
                if (elementIds == null || string.IsNullOrEmpty(familyName))
                    throw new ArgumentException("elementIds and familyName are required");

                _handler.ElementIds = elementIds;
                _handler.TargetFamilyName = familyName;

                if (RaiseAndWaitForCompletion(30000))
                    return _handler.Result;
                else
                    throw new TimeoutException("Change window type timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Change window type failed: {ex.Message}");
            }
        }
    }
}
