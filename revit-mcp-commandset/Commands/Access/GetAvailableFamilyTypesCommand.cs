using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;

namespace RevitMCPCommandSet.Commands.Access
{
    public class GetAvailableFamilyTypesCommand : ExternalEventCommandBase
    {
        private GetAvailableFamilyTypesEventHandler _handler => (GetAvailableFamilyTypesEventHandler)Handler;

        public override string CommandName => "get_available_family_types";

        public GetAvailableFamilyTypesCommand(UIApplication uiApp)
            : base(new GetAvailableFamilyTypesEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                // Parse parameters
                List<string> categoryList = parameters?["categoryList"]?.ToObject<List<string>>() ?? new List<string>();
                string familyNameFilter = parameters?["familyNameFilter"]?.Value<string>();
                int? limit = parameters?["limit"]?.Value<int>();

                // Apply the query parameters
                _handler.SetParameters(categoryList, familyNameFilter, limit);

                // Raise the external event and wait for completion, up to 15 seconds
                if (RaiseAndWaitForCompletion(15000))
                {
                    return _handler.ResultFamilyTypes;
                }
                else
                {
                    throw new TimeoutException("Timed out while retrieving available family types");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to retrieve available family types: {ex.Message}");
            }
        }
    }
}
