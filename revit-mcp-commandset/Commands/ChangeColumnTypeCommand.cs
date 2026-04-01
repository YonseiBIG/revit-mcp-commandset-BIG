using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;
using System.Collections.Generic;

namespace RevitMCPCommandSet.Commands
{
    public class ChangeColumnTypeCommand : ExternalEventCommandBase
    {
        private ChangeFamilyInstanceTypeEventHandler _handler => (ChangeFamilyInstanceTypeEventHandler)Handler;

        public override string CommandName => "change_column_type";

        public ChangeColumnTypeCommand(UIApplication uiApp)
            : base(new ChangeFamilyInstanceTypeEventHandler(Autodesk.Revit.DB.BuiltInCategory.OST_StructuralColumns), uiApp)
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

                _handler.ElementIds = elementIds;
                _handler.TargetSymbolName = symbolName;

                if (RaiseAndWaitForCompletion(15000))
                    return _handler.Result;
                else
                    throw new TimeoutException("Change column type timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Change column type failed: {ex.Message}");
            }
        }
    }
}
