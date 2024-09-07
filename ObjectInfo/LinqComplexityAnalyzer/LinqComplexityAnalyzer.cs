using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ObjectInfo.DeepDive.Analyzers;
using ObjectInfo.DeepDive.Analysis;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ObjectInfo.DeepDive.LinqComplexityAnalyzer
{
    public class LinqComplexityAnalyzer : IAnalyzer
    {
        private readonly ILogger _logger;
        private static readonly HashSet<string> LinqMethods = new HashSet<string> 
        { 
            "Where", "Select", "SelectMany", "OrderBy", "OrderByDescending", 
            "ThenBy", "ThenByDescending", "GroupBy", "Join", "GroupJoin", 
            "Take", "Skip", "Distinct", "Reverse", "ToList", "ToArray", "ToDictionary"
        };

        public LinqComplexityAnalyzer(ILogger logger)
        {
            _logger = logger;
        }

        public string Name => "LINQ Complexity Analyzer";

        public async Task<AnalysisResult> AnalyzeAsync(AnalysisContext context)
        {
            _logger.Information("LinqComplexityAnalyzer.AnalyzeAsync started");
            var methodResults = new List<string>();
            foreach (var methodInfo in context.ExtendedMethods)
            {
                var result = await AnalyzeMethodAsync(methodInfo);
                methodResults.Add(result);
            }
            var summary = $"Analyzed {methodResults.Count} methods for LINQ complexity.";
            var details = string.Join("\n\n", methodResults);
            _logger.Information("LinqComplexityAnalyzer.AnalyzeAsync completed");
            return new AnalysisResult(Name, summary, details);
        }

        private async Task<string> AnalyzeMethodAsync(IExtendedMethodInfo methodInfo)
        {
            _logger.Information($"Analyzing method: {methodInfo.Name}");

            string methodBody = methodInfo.GetMethodBody();
            _logger.Information($"Method body: {methodBody}");

            if (string.IsNullOrEmpty(methodBody))
            {
                _logger.Warning($"Method body is empty for method: {methodInfo.Name}");
                return $"Method: {methodInfo.Name}\nUnable to analyze: Method body is empty.";
            }

            var root = CSharpSyntaxTree.ParseText(methodBody).GetCompilationUnitRoot();
            
            var linqOperations = new List<string>();
            linqOperations.AddRange(GetMethodSyntaxLinqOperations(root));
            linqOperations.AddRange(GetQuerySyntaxLinqOperations(root));

            int complexity = linqOperations.Count;

            var result = $"Method: {methodInfo.Name}\n" +
                         $"LINQ Complexity: {GetComplexityLevel(complexity)} ({complexity} operations)\n" +
                         $"Operations: {string.Join(", ", linqOperations)}\n" +
                         $"Suggestions:\n{GenerateSuggestions(linqOperations)}";

            _logger.Information($"Completed analysis for method: {methodInfo.Name}, Complexity: {complexity}");
            return result;
        }

        private IEnumerable<string> GetMethodSyntaxLinqOperations(SyntaxNode root)
        {
            return root.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Where(i => IsLinqMethod(i))
                .Select(i => i.Expression.ToString().Split('.').Last())
                .Distinct();
        }

        private IEnumerable<string> GetQuerySyntaxLinqOperations(SyntaxNode root)
        {
            var queryExpressions = root.DescendantNodes().OfType<QueryExpressionSyntax>();
            var operations = new List<string>();

            foreach (var query in queryExpressions)
            {
                if (query.FromClause != null) operations.Add("From");
                operations.AddRange(query.Body.Clauses.OfType<WhereClauseSyntax>().Select(_ => "Where"));
                operations.AddRange(query.Body.Clauses.OfType<OrderByClauseSyntax>().Select(_ => "OrderBy"));
                operations.AddRange(query.Body.Clauses.OfType<SelectClauseSyntax>().Select(_ => "Select"));
            }

            return operations.Distinct();
        }

        private bool IsLinqMethod(InvocationExpressionSyntax invocation)
        {
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                return LinqMethods.Contains(memberAccess.Name.Identifier.Text);
            }
            return false;
        }

        private string GetComplexityLevel(int complexity)
        {
            return complexity switch
            {
                <= 3 => "Low",
                <= 7 => "Medium",
                _ => "High"
            };
        }

        private string GenerateSuggestions(List<string> operations)
        {
            var suggestions = new List<string>();

            if (operations.Contains("Where") && operations.Contains("SelectMany"))
                suggestions.Add("- Consider moving Where before SelectMany for better performance.");

            if (operations.Contains("OrderBy") && operations.Contains("Take"))
                suggestions.Add("- The OrderBy before Take might be inefficient for large datasets.");

            if (operations.Count > 5)
                suggestions.Add("- Consider breaking down complex LINQ queries into smaller, more manageable parts.");

            if (operations.Contains("GroupBy") && operations.Contains("OrderBy"))
                suggestions.Add("- Be cautious of ordering after grouping, as it can impact performance.");

            return suggestions.Count > 0 ? string.Join("\n", suggestions) : "- No specific suggestions.";
        }
    }
}
