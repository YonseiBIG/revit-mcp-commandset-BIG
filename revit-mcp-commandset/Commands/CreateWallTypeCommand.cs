using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Services;
using RevitMCPSDK.API.Base;
using System;

namespace RevitMCPCommandSet.Commands
{
    public class CreateWallTypeCommand : ExternalEventCommandBase
    {
        private CreateWallTypeEventHandler _handler => (CreateWallTypeEventHandler)Handler;

        public override string CommandName => "create_wall_type";

        public CreateWallTypeCommand(UIApplication uiApp)
            : base(new CreateWallTypeEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                var info = parameters.ToObject<WallTypeCreationInfo>();
                if (info == null || string.IsNullOrEmpty(info.WallTypeName) || info.Layers == null || info.Layers.Count == 0)
                    throw new ArgumentException("wallTypeName and layers are required");

                _handler.SetParameters(info);

                if (RaiseAndWaitForCompletion(20000))
                    return _handler.Result;
                else
                    throw new TimeoutException("Create wall type timed out");
            }
            catch (Exception ex)
            {
                throw new Exception($"Create wall type failed: {ex.Message}");
            }
        }
    }
}
