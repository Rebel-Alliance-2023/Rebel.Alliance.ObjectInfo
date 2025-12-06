using Microsoft.CodeAnalysis;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Core
{
    /// <summary>
    /// Abstraction for source production context to enable testing.
    /// </summary>
    public interface ISourceProductionContext
    {
        /// <summary>
        /// Adds generated source code to the compilation.
        /// </summary>
        /// <param name="hintName">The hint name for the generated file.</param>
        /// <param name="source">The source code content.</param>
        void AddSource(string hintName, string source);

        /// <summary>
        /// Reports a diagnostic.
        /// </summary>
        /// <param name="diagnostic">The diagnostic to report.</param>
        void ReportDiagnostic(Diagnostic diagnostic);

        /// <summary>
        /// Reports a diagnostic with a descriptor.
        /// </summary>
        /// <param name="descriptor">The diagnostic descriptor.</param>
        /// <param name="location">The optional location.</param>
        /// <param name="args">The message arguments.</param>
        void ReportDiagnostic(DiagnosticDescriptor descriptor, Location? location = null, params object?[] args);

        /// <summary>
        /// Gets the cancellation token.
        /// </summary>
        CancellationToken CancellationToken { get; }
    }
}


