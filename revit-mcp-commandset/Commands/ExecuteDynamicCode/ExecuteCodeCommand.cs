using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Commands.ExecuteDynamicCode
{
    /// <summary>
    /// 处理代码执行的命令类
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
                // 参数验证
                if (!parameters.ContainsKey("code"))
                {
                    throw new ArgumentException("Missing required parameter: 'code'");
                }

                // 解析代码和参数
                string code = parameters["code"].Value<string>();
                JArray parametersArray = parameters["parameters"] as JArray;
                object[] executionParameters = parametersArray?.ToObject<object[]>() ?? Array.Empty<object>();

                // 设置执行参数
                _handler.SetExecutionParameters(code, executionParameters);

                // 触发外部事件
                _externalEvent.Raise();

                // 等待完成
                if (_handler.WaitForCompletion(60000)) // 1分钟超时
                {
                    return _handler.ResultInfo;
                }
                else
                {
                    throw new TimeoutException("代码执行超时");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"执行代码失败: {ex.Message}", ex);
            }
        }
    }
}
