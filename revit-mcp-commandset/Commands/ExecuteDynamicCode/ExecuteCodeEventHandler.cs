using System.CodeDom.Compiler;
using Autodesk.Revit.UI;
using Microsoft.CSharp;
using Newtonsoft.Json;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Commands.ExecuteDynamicCode
{
    /// <summary>
    /// External event handler that processes code execution
    /// </summary>
    public class ExecuteCodeEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        // Code execution parameters
        private string _generatedCode;
        private object[] _executionParameters;

        // Execution result info
        public ExecutionResultInfo ResultInfo { get; private set; }

        // State synchronization object
        public bool TaskCompleted { get; private set; }
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        // Set the code and parameters to execute
        public void SetExecutionParameters( string code, object[] parameters = null )
        {
            _generatedCode = code;
            _executionParameters = parameters ?? Array.Empty<object>();
            TaskCompleted = false;
            _resetEvent.Reset();
        }

        // Wait for execution to complete - IWaitableExternalEventHandler interface implementation
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

                using (var transaction = new Transaction(doc, "Execute AI Code"))
                {
                    // Start transaction if not already in one (defensive)
                    if (transaction.GetStatus() != TransactionStatus.Started)
                        transaction.Start();

                    // Dynamically compile and execute the code
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
            // Wrap the code to provide a standard entry point
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
            // User code entry point
            {code}
            return null;
        }}
    }}
}}";

            var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(wrappedCode);

            // Get referenced assemblies
            var references = new List<Microsoft.CodeAnalysis.MetadataReference>
            {
                Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(Document).Assembly.Location), // RevitAPI.dll
                Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(UIApplication).Assembly.Location) // RevitAPIUI.dll
            };

            // Add dependent system assemblies (System.Runtime, System.Collections, etc.)
            // In .NET Core / .NET 5+, the core libraries are spread across many files, so a common approach is to load all assemblies referenced by the current AppDomain
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

                    throw new Exception($"Code compilation error:\n{errors}");
                }

                ms.Seek(0, System.IO.SeekOrigin.Begin);
                var assembly = System.Reflection.Assembly.Load(ms.ToArray());

                // Invoke the execution method via reflection
                var executorType = assembly.GetType("AIGeneratedCode.CodeExecutor");
                var executeMethod = executorType.GetMethod("Execute");

                return executeMethod.Invoke(null, new object[] { doc, parameters });
            }
        }

        public string GetName( )
        {
            return "Execute AI Code";
        }
    }

    // Execution result data structure
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
