using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;
using System.Collections.Generic;

namespace RevitMCPCommandSet.Commands
{
    public class ChangeBeamTypeCommand : ExternalEventCommandBase
    {
        private ChangeFamilyInstanceTypeEventHandler _handler => (ChangeFamilyInstanceTypeEventHandler)Handler;

        public override string CommandName => "change_beam_type";

        public ChangeBeamTypeCommand(UIApplication uiApp)
            : base(new ChangeFamilyInstanceTypeEventHandler(Autodesk.Revit.DB.BuiltInCategory.OST_StructuralFraming), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var elementIds = parameters["elementIds"]?.ToObject<List<int>>();
                var symbolName = parameters["familySymbolName"]?.Value<string>();
                if (elementIds == null || string.IsNullOrEmpty(symbolName))
                    throw new ArgumentException("elementIds and familySymbolName are required");

                _handler.SetParameters(elementIds, symbolName);

                if (RaiseAndWaitForCompletion(15000))
                    return _handler.Result;
                else
                    throw new TimeoutException("Change beam type timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Change beam type failed: {ex.Message}");
            }
        }
    }
}
