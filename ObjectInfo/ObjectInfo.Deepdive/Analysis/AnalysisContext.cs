using ObjectInfo.Models.ObjectInfo;

namespace ObjectInfo.DeepDive.Analysis
{
    public class AnalysisContext
    {
        public ObjInfo Target { get; }

        public AnalysisContext(ObjInfo target)
        {
            Target = target;
        }
    }
}
