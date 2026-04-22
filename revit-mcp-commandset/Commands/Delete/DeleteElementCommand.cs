//using Autodesk.Revit.UI;
//using Newtonsoft.Json.Linq;
//using RevitMCPCommandSet.Services;
//using RevitMCPSDK.API.Base;
//using RevitMCPSDK.API.Models;
//using RevitMCPSDK.Exceptions;

//namespace RevitMCPCommandSet.Commands.Delete
//{
//    public class DeleteElementCommand : ExternalEventCommandBase
//    {
//        private DeleteElementEventHandler _handler => (DeleteElementEventHandler)Handler;
//        public override string CommandName => "delete_element";
//        public DeleteElementCommand(UIApplication uiApp)
//            : base(new DeleteElementEventHandler(), uiApp)
//        {
//        }
//        public override object Execute(JObject parameters, string requestId)
//        {
//            try
//            {
//                // Parse array parameter
//                var elementIds = parameters?["elementIds"]?.ToObject<string[]>();
//                if (elementIds == null || elementIds.Length == 0)
//                {
//                    throw new CommandExecutionException(
//                        "Element ID list cannot be empty",
//                        JsonRPCErrorCodes.InvalidParams);
//                }
//                // Set the array of element IDs to delete
//                _handler.ElementIds = elementIds;
//                // Raise the external event and wait for completion
//                if (RaiseAndWaitForCompletion(15000))
//                {
//                    if (_handler.IsSuccess)
//                    {
//                        return CommandResult.CreateSuccess(new { deleted = true, count = _handler.DeletedCount });
//                    }
//                    else
//                    {
//                        throw new CommandExecutionException(
//                            "Failed to delete elements",
//                            JsonRPCErrorCodes.ElementDeletionFailed);
//                    }
//                }
//                else
//                {
//                    throw CreateTimeoutException(CommandName);
//                }
//            }
//            catch (CommandExecutionException)
//            {
//                throw;
//            }
//            catch (Exception ex)
//            {
//                throw new CommandExecutionException(
//                    $"Failed to delete elements: {ex.Message}",
//                    JsonRPCErrorCodes.InternalError);
//            }
//        }
//    }
//}
