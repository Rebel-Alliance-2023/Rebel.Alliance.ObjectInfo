using ObjectInfo.Models.ObjectInfo;
using ObjectInfo.DeepDive.Analyzers;
using ObjectInfo.DeepDive.Analysis;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ObjectInfo.DeepDive
{
    public class DeepDiveAnalysis
    {
        private readonly ObjInfo _baseObjectInfo;
        private readonly AnalyzerManager _analyzerManager;
        private readonly ILogger _logger;

        public DeepDiveAnalysis(ObjInfo baseObjectInfo, AnalyzerManager analyzerManager, ILogger logger)
        {
            _baseObjectInfo = baseObjectInfo;
            _analyzerManager = analyzerManager;
            _logger = logger;
        }

        public async Task<IEnumerable<AnalysisResult>> RunAllAnalyzersAsync()
        {
            _logger.Information("DeepDiveAnalysis.RunAllAnalyzersAsync started");
            var results = await _analyzerManager.RunAnalyzersAsync(_baseObjectInfo);
            _logger.Information($"DeepDiveAnalysis.RunAllAnalyzersAsync completed. Results count: {results.Count()}");
            return results;
        }
    }
}
