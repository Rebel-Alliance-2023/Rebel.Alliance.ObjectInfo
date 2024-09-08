using ObjectInfo.Models.MethodInfo;

namespace ObjectInfo.DeepDive.Analysis
{
    /// <summary>
    /// Provides extended information about a method, including its body.
    /// </summary>
    public interface IExtendedMethodInfo : IMethodInfo
    {
        /// <summary>
        /// Gets the body of the method as a string.
        /// </summary>
        /// <returns>The method body as a string.</returns>
        string GetMethodBody();
    }
}
