using Microsoft.CodeAnalysis;
using Xunit.Abstractions;

namespace SpecificationGenerator.Tests.TestInfrastructure
{
    public class TestSourceProductionContextWrapper
    {
        private readonly TestSourceProductionContext _testContext;

        public TestSourceProductionContextWrapper(TestSourceProductionContext testContext)
        {
            _testContext = testContext;
        }

        public void AddSource(string hintName, string source)
        {
            _testContext.AddSource(hintName, source);
        }

        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            _testContext.ReportDiagnostic(diagnostic);
        }

        public CancellationToken CancellationToken => _testContext.CancellationToken;
    }
}
