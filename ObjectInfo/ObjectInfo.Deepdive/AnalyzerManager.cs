using ObjectInfo.Models.ObjectInfo;
using ObjectInfo.DeepDive.Analyzers;
using ObjectInfo.DeepDive.Analysis;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ObjectInfo.DeepDive
{
    public class AnalyzerManager
    {
        private readonly IEnumerable<IAnalyzer> _analyzers;
        private readonly ILogger _logger;

        public AnalyzerManager(IEnumerable<IAnalyzer> analyzers, ILogger logger)
        {
            _analyzers = analyzers;
            _logger = logger;
        }

        public async Task<IEnumerable<AnalysisResult>> RunAnalyzersAsync(ObjInfo objInfo)
        {
            _logger.Information($"AnalyzerManager.RunAnalyzersAsync started for type: {objInfo.TypeInfo.Name}");
            _logger.Information($"Number of registered analyzers: {_analyzers.Count()}");

            var results = new List<AnalysisResult>();
            var context = new AnalysisContext(objInfo);

            foreach (var analyzer in _analyzers)
            {
                _logger.Information($"Running analyzer: {analyzer.GetType().Name}");
                try
                {
                    var result = await analyzer.AnalyzeAsync(context);
                    if (result != null)
                    {
                        _logger.Information($"Analyzer {analyzer.GetType().Name} returned a result");
                        results.Add(result);
                    }
                    else
                    {
                        _logger.Warning($"Analyzer {analyzer.GetType().Name} returned null");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Error running analyzer {analyzer.GetType().Name}");
                }
            }

            _logger.Information($"AnalyzerManager.RunAnalyzersAsync completed. Total results: {results.Count}");
            return results;
        }
    }
}
