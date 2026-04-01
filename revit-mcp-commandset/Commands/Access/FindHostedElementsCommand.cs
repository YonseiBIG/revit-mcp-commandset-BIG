using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;
using System.Collections.Generic;

namespace RevitMCPCommandSet.Commands.Access
{
    public class FindHostedElementsCommand : ExternalEventCommandBase
    {
        private FindHostedElementsEventHandler _handler => (FindHostedElementsEventHandler)Handler;

        public override string CommandName => "find_hosted_elements";

        public FindHostedElementsCommand(UIApplication uiApp)
            : base(new FindHostedElementsEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var hostElementIds = parameters["hostElementIds"]?.ToObject<List<int>>();
                var category = parameters["category"]?.Value<string>();
                if (hostElementIds == null || string.IsNullOrEmpty(category))
                    throw new ArgumentException("hostElementIds and category are required");

                _handler.HostElementIds = hostElementIds;
                _handler.Category = category;

                if (RaiseAndWaitForCompletion(15000))
                {
                    return _handler.Result;
                }
                else
                {
                    throw new TimeoutException("Find hosted elements timed out");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Find hosted elements failed: {ex.Message}");
            }
        }
    }
}
