using ObjectInfo.DeepDive.Analyzers;
using ObjectInfo.DeepDive.Analysis;
using ObjectInfo.Models.ObjectInfo;
using Serilog;

namespace ObjectInfo.DeepDive
{
    /// <summary>
    /// Manages and executes analyzers for deep dive analysis.
    /// </summary>
    public class AnalyzerManager
    {
        private readonly IEnumerable<IAnalyzer> _analyzers;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnalyzerManager"/> class.
        /// </summary>
        /// <param name="analyzers">The collection of analyzers.</param>
        /// <param name="logger">The logger instance.</param>
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

        /// <summary>
        /// Runs all registered analyzers on the specified object information.
        /// </summary>
        /// <param name="objInfo">The object information to analyze.</param>
        /// <returns>The collection of analysis results.</returns>
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

        /// <summary>
        /// Runs a specific analyzer by name on the provided context.
        /// </summary>
        /// <param name="analyzerName">The name of the analyzer to run.</param>
        /// <param name="context">The analysis context.</param>
        /// <returns>The analysis result.</returns>
        public async Task<AnalysisResult> RunAnalyzerAsync(string analyzerName, AnalysisContext context)
        {
            var analyzer = GetAnalyzer(analyzerName);
            return await analyzer.AnalyzeAsync(context);
        }
    }
}
