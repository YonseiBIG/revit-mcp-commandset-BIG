using System.CodeDom.Compiler;
using Autodesk.Revit.UI;
using Microsoft.CSharp;
using Newtonsoft.Json;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Commands.ExecuteDynamicCode
{
    /// <summary>
    /// 处理代码执行的外部事件处理器
    /// </summary>
    public class ExecuteCodeEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        // 代码执行参数
        private string _generatedCode;
        private object[] _executionParameters;

        // 执行结果信息
        public ExecutionResultInfo ResultInfo { get; private set; }

        // 状态同步对象
        public bool TaskCompleted { get; private set; }
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        // 设置要执行的代码和参数
        public void SetExecutionParameters( string code, object[] parameters = null )
        {
            _generatedCode = code;
            _executionParameters = parameters ?? Array.Empty<object>();
            TaskCompleted = false;
            _resetEvent.Reset();
        }

        // 等待执行完成 - IWaitableExternalEventHandler接口实现
        public bool WaitForCompletion( int timeoutMilliseconds = 10000 )
        {
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        public void Execute( UIApplication app )
        {
            try
            {
                var doc = app.ActiveUIDocument.Document;
                ResultInfo = new ExecutionResultInfo();

                using (var transaction = new Transaction(doc, "执行AI代码"))
                {
                    // Start transaction if not already in one (defensive)
                    if (transaction.GetStatus() != TransactionStatus.Started)
                        transaction.Start();

                    // 动态编译执行代码
                    var result = CompileAndExecuteCode(
                        code: _generatedCode,
                        doc: doc,
                        parameters: _executionParameters
                    );

                    transaction.Commit();

                    ResultInfo.Success = true;
                    ResultInfo.Result = JsonConvert.SerializeObject(result);
                }
            }
            catch (Exception ex)
            {
                ResultInfo.Success = false;
                // Return FULL stack trace and inner exception to help debugging
                ResultInfo.ErrorMessage = $"Error: {ex.Message}\nType: {ex.GetType().Name}\nStack: {ex.StackTrace}";
                if (ex.InnerException != null)
                {
                    ResultInfo.ErrorMessage += $"\nInner: {ex.InnerException.Message}";
                }
            }
            finally
            {
                TaskCompleted = true;
                _resetEvent.Set();
            }
        }

        private object CompileAndExecuteCode( string code, Document doc, object[] parameters )
        {
            // 包装代码以规范入口点
            var wrappedCode = $@"
using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Linq;

namespace AIGeneratedCode
{{
    public static class CodeExecutor
    {{
        public static object Execute(Document document, object[] parameters)
        {{
            // 用户代码入口
            {code}
            return null;
        }}
    }}
}}";

            var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(wrappedCode);

            // 获取引用程序集
            var references = new List<Microsoft.CodeAnalysis.MetadataReference>
            {
                Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(Document).Assembly.Location), // RevitAPI.dll
                Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(UIApplication).Assembly.Location) // RevitAPIUI.dll
            };

            // 添加依赖的系统程序集 (System.Runtime, System.Collections, etc.)
            // 在 .NET Core / .NET 5+ 中，核心库位置比较分散，比较通用的做法是加载当前 AppDomain 的所有引用
            try
            {
                var trustedAssemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))?.Split(System.IO.Path.PathSeparator);
                if (trustedAssemblies != null)
                {
                    foreach (var assemblyPath in trustedAssemblies)
                    {
                        try
                        {
                            // Filter out some obviously unnecessary or problematic files if needed
                            if (!string.IsNullOrEmpty(assemblyPath) && System.IO.File.Exists(assemblyPath))
                            {
                                references.Add(Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(assemblyPath));
                            }
                        }
                        catch { /* Ignore invalid assemblies */ }
                    }
                }
            }
            catch (Exception ex)
            {
                // Fallback: try to load at least core basics if the above fails
                Console.WriteLine("Warning: Failed to load trusted assemblies: " + ex.Message);
            }

            var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create(
                "AIGeneratedAssembly_" + Guid.NewGuid(),
                new[] { syntaxTree },
                references,
                new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary));

            using (var ms = new System.IO.MemoryStream())
            {
                var result = compilation.Emit(ms);

                if (!result.Success)
                {
                    var errors = string.Join("\n", result.Diagnostics
                        .Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                        .Select(diagnostic => $"Line {diagnostic.Location.GetLineSpan().StartLinePosition.Line}: {diagnostic.Id}: {diagnostic.GetMessage()}"));

                    throw new Exception($"代码编译错误:\n{errors}");
                }

                ms.Seek(0, System.IO.SeekOrigin.Begin);
                var assembly = System.Reflection.Assembly.Load(ms.ToArray());

                // 反射调用执行方法
                var executorType = assembly.GetType("AIGeneratedCode.CodeExecutor");
                var executeMethod = executorType.GetMethod("Execute");

                return executeMethod.Invoke(null, new object[] { doc, parameters });
            }
        }

        public string GetName( )
        {
            return "执行AI代码";
        }
    }

    // 执行结果数据结构
    public class ExecutionResultInfo
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("result")]
        public string Result { get; set; }

        [JsonProperty("errorMessage")]
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
