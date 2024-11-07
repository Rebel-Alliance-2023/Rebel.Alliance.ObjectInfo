using Microsoft.CodeAnalysis;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Core
{
    public interface ISourceProductionContext
    {
        void AddSource(string hintName, string source);
        void ReportDiagnostic(Diagnostic diagnostic);
        void ReportDiagnostic(DiagnosticDescriptor descriptor, Location? location = null, params object?[] args);
        CancellationToken CancellationToken { get; }
    }
}


