using ObjectInfo.DeepDive.Analyzers;
using ObjectInfo.DeepDive.Plugins;
using System.Collections.Generic;

namespace ObjectInfo.DeepDive.LinqComplexityAnalyzer
{
    public class LinqComplexityAnalyzerPlugin : IAnalyzerPlugin
    {
        public string Name => "LINQ Complexity Analyzer Plugin";
        public string Version => "1.0.0";

        public IEnumerable<IAnalyzer> GetAnalyzers()
        {
            yield return new LinqComplexityAnalyzer(Serilog.Log.Logger);
        }
    }
}
