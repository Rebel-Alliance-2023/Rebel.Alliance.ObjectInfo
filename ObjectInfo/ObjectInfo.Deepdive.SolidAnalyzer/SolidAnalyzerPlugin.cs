using ObjectInfo.DeepDive.Analyzers;
using ObjectInfo.DeepDive.Plugins;
using System.Collections.Generic;
using Serilog;

namespace ObjectInfo.Deepdive.SolidAnalyzer
{
    public class SolidAnalyzerPlugin : IAnalyzerPlugin
    {
        public string Name => "SOLID Analyzer Plugin";
        public string Version => "1.0.0";

        public IEnumerable<IAnalyzer> GetAnalyzers()
        {
            yield return new SolidAnalyzer(Log.Logger);
        }
    }
}