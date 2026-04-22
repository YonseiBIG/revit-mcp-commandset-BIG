using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;
using System.Collections.Generic;

namespace RevitMCPCommandSet.Commands.Access
{
    public class GetLevelsByNameCommand : ExternalEventCommandBase
    {
        private GetLevelsByNameEventHandler _handler => (GetLevelsByNameEventHandler)Handler;

        public override string CommandName => "get_levels_by_name";

        public GetLevelsByNameCommand(UIApplication uiApp)
            : base(new GetLevelsByNameEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var levelNames = parameters["levelNames"]?.ToObject<List<string>>();
                if (levelNames == null || levelNames.Count == 0)
                    throw new ArgumentException("levelNames is required");

                _handler.SetParameters(levelNames);

                if (RaiseAndWaitForCompletion(15000))
                {
                    return _handler.Result;
                }
                else
                {
                    throw new TimeoutException("Get levels by name timed out");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Get levels by name failed: {ex.Message}");
            }
        }
    }
}
