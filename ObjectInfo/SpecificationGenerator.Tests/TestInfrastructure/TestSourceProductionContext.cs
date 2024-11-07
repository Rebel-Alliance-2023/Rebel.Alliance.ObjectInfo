using Microsoft.CodeAnalysis;
using Xunit.Abstractions;

using ObjectInfo.Deepdive.SpecificationGenerator.Core;

namespace SpecificationGenerator.Tests.TestInfrastructure
{
    public class TestSourceProductionContext : ISourceProductionContext
    {
        private readonly ITestOutputHelper _output;

        private readonly ISourceProductionContext _innerContext;

        public TestSourceProductionContext(ITestOutputHelper output, ISourceProductionContext innerContext)
        {
            _innerContext = innerContext;
            _output = output;
        }

        public void AddSource(string hintName, string source)
        {
            _output.WriteLine($"Generated source for {hintName}:");
            _output.WriteLine(source);
        }

        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            _output.WriteLine($"Diagnostic: {diagnostic}");
        }

        public void ReportDiagnostic(DiagnosticDescriptor descriptor, Location? location = null, params object?[] args)
        {
            _innerContext.ReportDiagnostic(descriptor, location, args);
        }

        public CancellationToken CancellationToken => CancellationToken.None;
    }
}
