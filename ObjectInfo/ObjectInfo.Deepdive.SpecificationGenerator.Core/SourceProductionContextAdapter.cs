using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Core
{
    /// <summary>
    /// Adapts the Roslyn SourceProductionContext to the ISourceProductionContext interface.
    /// </summary>
    public class SourceProductionContextAdapter : ISourceProductionContext
    {
        private readonly Microsoft.CodeAnalysis.SourceProductionContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="SourceProductionContextAdapter"/> class.
        /// </summary>
        /// <param name="context">The Roslyn source production context.</param>
        public SourceProductionContextAdapter(Microsoft.CodeAnalysis.SourceProductionContext context)
        {
            _context = context;
        }

        /// <inheritdoc/>
        public void AddSource(string hintName, string source)
        {
            _context.AddSource(hintName, source);
        }

        /// <inheritdoc/>
        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            _context.ReportDiagnostic(diagnostic);
        }

        /// <inheritdoc/>
        public void ReportDiagnostic(DiagnosticDescriptor descriptor, Location? location = null, params object?[] args)
        {
            _context.ReportDiagnostic(Diagnostic.Create(descriptor, location, args));
        }

        /// <inheritdoc/>
        public CancellationToken CancellationToken => _context.CancellationToken;
    }

}
