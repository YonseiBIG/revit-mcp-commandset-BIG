using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Commands.ExecuteDynamicCode
{
    /// <summary>
    /// Command class that handles code execution
    /// </summary>
    public class ExecuteCodeCommand : IRevitCommand
    {
        private readonly ExecuteCodeEventHandler _handler;
        private readonly ExternalEvent _externalEvent;

        public string CommandName => "send_code_to_revit";

        public ExecuteCodeCommand( UIApplication uiApp )
        {
            _handler = new ExecuteCodeEventHandler();
            _externalEvent = ExternalEvent.Create(_handler);
        }

        public object Execute( JObject parameters, string requestId )
        {
            try
            {
                // Parameter validation
                if (!parameters.ContainsKey("code"))
                {
                    throw new ArgumentException("Missing required parameter: 'code'");
                }

                // Parse code and parameters
                string code = parameters["code"].Value<string>();
                JArray parametersArray = parameters["parameters"] as JArray;
                object[] executionParameters = parametersArray?.ToObject<object[]>() ?? Array.Empty<object>();

                // Apply the execution parameters
                _handler.SetExecutionParameters(code, executionParameters);

                // Raise the external event
                _externalEvent.Raise();

                // Wait for completion
                if (_handler.WaitForCompletion(60000)) // 1 minute timeout
                {
                    return _handler.ResultInfo;
                }
                else
                {
                    throw new TimeoutException("Code execution timed out");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to execute code: {ex.Message}", ex);
            }
        }
    }
}
