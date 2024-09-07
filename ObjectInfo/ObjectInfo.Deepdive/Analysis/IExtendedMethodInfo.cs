using ObjectInfo.Models.MethodInfo;

namespace ObjectInfo.DeepDive.Analysis
{
    public interface IExtendedMethodInfo : IMethodInfo
    {
        string GetMethodBody();
    }
}
