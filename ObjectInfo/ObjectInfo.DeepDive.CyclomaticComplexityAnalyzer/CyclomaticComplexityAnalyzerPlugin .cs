using ObjectInfo.DeepDive.Analyzers;
using ObjectInfo.DeepDive.Plugins;
using System.Collections.Generic;

namespace ObjectInfo.DeepDive.CyclomaticComplexityAnalyzer
{
    public class CyclomaticComplexityAnalyzerPlugin : IAnalyzerPlugin
    {
        public string Name => "Cyclomatic Complexity Analyzer Plugin";
        public string Version => "1.0.0";

        public IEnumerable<IAnalyzer> GetAnalyzers()
        {
            yield return new CyclomaticComplexityAnalyzer(Serilog.Log.Logger);
        }
    }
}
