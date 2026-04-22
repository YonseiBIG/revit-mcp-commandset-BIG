using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;
using System.Collections.Generic;

namespace RevitMCPCommandSet.Commands.Access
{
    public class FindElementsByLevelCommand : ExternalEventCommandBase
    {
        private FindElementsByLevelEventHandler _handler => (FindElementsByLevelEventHandler)Handler;

        public override string CommandName => "find_elements_by_level";

        public FindElementsByLevelCommand(UIApplication uiApp)
            : base(new FindElementsByLevelEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var levelNames = parameters["levelNames"]?.ToObject<List<string>>();
                var category = parameters["category"]?.Value<string>();
                if (levelNames == null || string.IsNullOrEmpty(category))
                    throw new ArgumentException("levelNames and category are required");

                _handler.SetParameters(levelNames, category);

                if (RaiseAndWaitForCompletion(15000))
                {
                    return _handler.Result;
                }
                else
                {
                    throw new TimeoutException("Find elements by level timed out");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Find elements by level failed: {ex.Message}");
            }
        }
    }
}
