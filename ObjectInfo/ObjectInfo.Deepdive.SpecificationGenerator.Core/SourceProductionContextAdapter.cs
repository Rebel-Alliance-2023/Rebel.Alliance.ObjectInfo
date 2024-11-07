using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Core
{
    public class SourceProductionContextAdapter : ISourceProductionContext
    {
        private readonly Microsoft.CodeAnalysis.SourceProductionContext _context;

        public SourceProductionContextAdapter(Microsoft.CodeAnalysis.SourceProductionContext context)
        {
            _context = context;
        }

        public void AddSource(string hintName, string source)
        {
            _context.AddSource(hintName, source);
        }

        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            _context.ReportDiagnostic(diagnostic);
        }

        public void ReportDiagnostic(DiagnosticDescriptor descriptor, Location? location = null, params object?[] args)
        {
            _context.ReportDiagnostic(Diagnostic.Create(descriptor, location, args));
        }

        public CancellationToken CancellationToken => _context.CancellationToken;
    }

}
