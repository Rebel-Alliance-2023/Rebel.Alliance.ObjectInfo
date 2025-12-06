using System;
using System.Reflection;
using Serilog;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.TypeSystem;
using ObjectInfo.Models.TypeInfo;
using ObjectInfo.Models.MethodInfo;


namespace ObjectInfo.DeepDive.Analysis
{
    /// <summary>
    /// Provides extended information about a method, including decompilation capabilities.
    /// </summary>
    public class ExtendedMethodInfo : IExtendedMethodInfo
    {
        private readonly IMethodInfo _baseMethodInfo;
        private readonly ILogger _logger;
        private readonly Assembly _testAssembly;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtendedMethodInfo"/> class.
        /// </summary>
        /// <param name="baseMethodInfo">The base method information.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="testAssembly">The assembly containing the method.</param>
        public ExtendedMethodInfo(IMethodInfo baseMethodInfo, ILogger logger, Assembly testAssembly)
        {
            _baseMethodInfo = baseMethodInfo;
            _logger = logger;
            _testAssembly = testAssembly;
        }

        /// <summary>
        /// Gets or sets the declaring type name.
        /// </summary>
        public string DeclaringType 
        { 
            get => _baseMethodInfo.DeclaringType;
            set => _baseMethodInfo.DeclaringType = value;
        }

        /// <summary>
        /// Gets or sets the method name.
        /// </summary>
        public string Name 
        { 
            get => _baseMethodInfo.Name;
            set => _baseMethodInfo.Name = value;
        }

        /// <summary>
        /// Gets or sets the reflected type name.
        /// </summary>
        public string ReflectedType 
        { 
            get => _baseMethodInfo.ReflectedType;
            set => _baseMethodInfo.ReflectedType = value;
        }

        /// <summary>
        /// Gets or sets the custom attributes.
        /// </summary>
        public List<ObjectInfo.Models.TypeInfo.ITypeInfo> CustomAttrs 
        { 
            get => _baseMethodInfo.CustomAttrs;
            set => _baseMethodInfo.CustomAttrs = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the method is virtual.
        /// </summary>
        public bool IsVirtual { get;set; }

        /// <summary>
        /// Gets the decompiled method body as source code.
        /// </summary>
        /// <returns>The decompiled method body.</returns>
        public string GetMethodBody()
        {
            try
            {
                // Load the assembly using Mono.Cecil
                var assemblyDefinition = AssemblyDefinition.ReadAssembly(_testAssembly.Location);
                string typeName = $"{_testAssembly.GetName().Name}.{DeclaringType}";
                FullTypeName fullTypeName = new FullTypeName(typeName);

                var typeDefinition = assemblyDefinition.MainModule.GetType(typeName);
                var methodDefinition = typeDefinition.Methods.First(m => m.Name == Name);

                // Check if the method has a body
                if (!methodDefinition.HasBody)
                {
                    _logger.Error($"Method {methodDefinition.Name} does not have a body.");
                    return $"// Error: Method {methodDefinition.Name} does not have a body.";
                }

                // Decompile the method body to C# using ICSharpCode.Decompiler
                var decompiler = new CSharpDecompiler(_testAssembly.Location, new DecompilerSettings());
                
                //FullTypeName fullTypeName = new FullTypeName(_methodInfo.DeclaringType.FullName);
                var decompiledCode = decompiler.DecompileTypeAsString(fullTypeName);

                // Extract the specific method's code from the decompiled type
                var methodCode = ExtractMethodCode(decompiledCode, Name);
                return methodCode;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error while decompiling method {Name}: {ex.Message}");
                return $"// Error: {ex.Message}";
            }
        }

        private string ExtractMethodCode(string decompiledTypeCode, string methodName)
        {
            // Simple extraction logic to get the method code from the decompiled type code
            var methodStart = decompiledTypeCode.IndexOf($"public IEnumerable<int> {methodName}");
            if (methodStart == -1)
            {
                return $"// Error: Method {methodName} not found in decompiled code.";
            }

            var methodEnd = decompiledTypeCode.IndexOf("}", methodStart) + 1;
            var methodCode = decompiledTypeCode.Substring(methodStart, methodEnd - methodStart);
            return methodCode;
        }
    }
}
