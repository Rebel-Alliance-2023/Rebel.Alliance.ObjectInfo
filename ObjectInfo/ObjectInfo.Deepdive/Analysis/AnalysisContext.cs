using ObjectInfo.Models.ObjectInfo;
using System.Collections.Generic;

namespace ObjectInfo.DeepDive.Analysis
{
    /// <summary>
    /// Provides context for analysis operations.
    /// </summary>
    public class AnalysisContext
    {
        /// <summary>
        /// Gets the target object information being analyzed.
        /// </summary>
        public ObjInfo Target { get; }

        /// <summary>
        /// Gets or sets the collection of extended method information.
        /// </summary>
        public IEnumerable<IExtendedMethodInfo> ExtendedMethods { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AnalysisContext"/> class.
        /// </summary>
        /// <param name="target">The target object information.</param>
        public AnalysisContext(ObjInfo target)
        {
            Target = target;
            ExtendedMethods = new List<IExtendedMethodInfo>();
        }
    }
}
