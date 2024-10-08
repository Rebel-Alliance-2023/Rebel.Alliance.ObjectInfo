using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ObjectInfo.DeepDive.Analyzers;
using ObjectInfo.DeepDive.Analysis;
using ObjectInfo.Models.ObjectInfo;
using ObjectInfo.Models.MethodInfo;
using System.Reflection;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ObjectInfo.DeepDive.CyclomaticComplexityAnalyzer
{
    public class CyclomaticComplexityAnalyzer : IMethodAnalyzer
    {
        private readonly ILogger _logger;

        public CyclomaticComplexityAnalyzer(ILogger logger)
        {
            _logger = logger;
        }

        public string Name => "Cyclomatic Complexity Analyzer";

        public async Task<AnalysisResult> AnalyzeAsync(AnalysisContext context)
        {
            _logger.Information("CyclomaticComplexityAnalyzer.AnalyzeAsync started");
            if (context.Target is ObjInfo objInfo)
            {
                var methodResults = new List<MethodAnalysisResult>();
                foreach (var methodInfo in objInfo.TypeInfo.MethodInfos)
                {
                    var result = await AnalyzeMethodAsync(methodInfo);
                    methodResults.Add(result);
                }
                var summary = $"Analyzed {methodResults.Count} methods for cyclomatic complexity.";
                var details = string.Join("\n", methodResults.Select(r => r.Details));
                _logger.Information("CyclomaticComplexityAnalyzer.AnalyzeAsync completed");
                return new AnalysisResult(Name, summary, details);
            }
            else
            {
                _logger.Warning("CyclomaticComplexityAnalyzer.AnalyzeAsync received invalid context");
                return new AnalysisResult(Name, "Invalid analysis target", "The provided context does not contain a valid ObjInfo instance.");
            }
        }

        public async Task<MethodAnalysisResult> AnalyzeMethodAsync(IMethodInfo methodInfo)
        {
            _logger.Information($"Analyzing method: {methodInfo.Name} in type: {methodInfo.DeclaringType}");

            string methodBody = GetMethodBody(methodInfo);
            int complexity = CalculateComplexity(methodBody);

            var summary = $"Cyclomatic Complexity for method {methodInfo.Name}: {complexity}";
            var details = $"Method: {methodInfo.Name}\nComplexity: {complexity}\n" +
                          $"Interpretation: {InterpretComplexity(complexity)}";

            _logger.Information($"Completed analysis for method: {methodInfo.Name}, Complexity: {complexity}");
            return new MethodAnalysisResult(Name, methodInfo.Name, summary, details, complexity);
        }

        private string GetMethodBody(IMethodInfo methodInfo)
        {
            _logger.Information($"Getting method body for: {methodInfo.Name} in type: {methodInfo.DeclaringType}");

            Type type = FindType(methodInfo.DeclaringType);
            if (type == null)
            {
                _logger.Error($"Failed to find type: {methodInfo.DeclaringType}");
                throw new InvalidOperationException($"Type {methodInfo.DeclaringType} not found");
            }

            var method = type.GetMethod(methodInfo.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            if (method == null)
            {
                _logger.Error($"Failed to find method: {methodInfo.Name} in type: {type.FullName}");
                throw new InvalidOperationException($"Method {methodInfo.Name} not found in type {methodInfo.DeclaringType}");
            }

            var methodBody = method.GetMethodBody();
            if (methodBody == null)
            {
                _logger.Error($"Failed to get method body for: {methodInfo.Name} in type: {type.FullName}");
                throw new InvalidOperationException($"Unable to retrieve method body for {methodInfo.Name}");
            }

            return BitConverter.ToString(methodBody.GetILAsByteArray());
        }

        private Type FindType(string typeName)
        {
            _logger.Information($"Searching for type: {typeName}");

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                _logger.Information($"Searching in assembly: {assembly.FullName}");
                var type = assembly.GetTypes()
                    .FirstOrDefault(t => t.FullName == typeName || t.Name == typeName);
                if (type != null)
                {
                    _logger.Information($"Found type {typeName} in assembly {assembly.FullName}");
                    return type;
                }
            }

            _logger.Error($"Type {typeName} not found in any loaded assembly");
            return null;
        }

        private int CalculateComplexity(string methodBody)
        {
            int complexity = 1;
            complexity += methodBody.Split('-').Count(b => b == "02");
            return complexity;
        }

        private string InterpretComplexity(int complexity)
        {
            return complexity switch
            {
                <= 5 => "Simple, low-risk code",
                <= 10 => "Moderately complex, moderate risk",
                <= 20 => "Complex, high-risk code",
                _ => "Untestable code (very high risk)"
            };
        }
    }
}
