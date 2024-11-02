using ObjectInfo.DeepDive.Analyzers;
using ObjectInfo.DeepDive.Analysis;
using ObjectInfo.Models.ObjectInfo;
using Serilog;

namespace ObjectInfo.DeepDive
{
    public class AnalyzerManager
    {
        private readonly IEnumerable<IAnalyzer> _analyzers;
        private readonly ILogger _logger;

        public AnalyzerManager(IEnumerable<IAnalyzer> analyzers, ILogger logger)
        {
            _analyzers = analyzers ?? throw new ArgumentNullException(nameof(analyzers));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets an analyzer by its name.
        /// </summary>
        /// <param name="analyzerName">The name of the analyzer to retrieve.</param>
        /// <returns>The requested analyzer, or throws if not found.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the analyzer is not found.</exception>
        public IAnalyzer GetAnalyzer(string analyzerName)
        {
            ArgumentException.ThrowIfNullOrEmpty(analyzerName);
            
            var analyzer = _analyzers.FirstOrDefault(a => a.Name.Equals(analyzerName, StringComparison.OrdinalIgnoreCase));
            if (analyzer == null)
            {
                throw new InvalidOperationException($"Analyzer not found: {analyzerName}");
            }
            
            return analyzer;
        }

        public async Task<IEnumerable<AnalysisResult>> RunAnalyzersAsync(ObjInfo objInfo)
        {
            ArgumentNullException.ThrowIfNull(objInfo);

            var results = new List<AnalysisResult>();
            foreach (var analyzer in _analyzers)
            {
                try
                {
                    var context = new AnalysisContext(objInfo);
                    var result = await analyzer.AnalyzeAsync(context);
                    results.Add(result);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error running analyzer {AnalyzerName}", analyzer.Name);
                }
            }

            return results;
        }

        public async Task<AnalysisResult> RunAnalyzerAsync(string analyzerName, AnalysisContext context)
        {
            var analyzer = GetAnalyzer(analyzerName);
            return await analyzer.AnalyzeAsync(context);
        }
    }
}
