using ObjectInfo.Models.ObjectInfo;
using System.Collections.Generic;

namespace ObjectInfo.DeepDive.Analysis
{
    public class AnalysisContext
    {
        public ObjInfo Target { get; }
        public IEnumerable<IExtendedMethodInfo> ExtendedMethods { get; set; }

        public AnalysisContext(ObjInfo target)
        {
            Target = target;
            ExtendedMethods = new List<IExtendedMethodInfo>();
        }
    }
}
