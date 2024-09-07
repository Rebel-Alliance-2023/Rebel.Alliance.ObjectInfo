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
  public class ExtendedMethodInfo : IExtendedMethodInfo
    {
        private readonly IMethodInfo _baseMethodInfo;
        private readonly ILogger _logger;
        private readonly Assembly _testAssembly;

        public ExtendedMethodInfo(IMethodInfo baseMethodInfo, ILogger logger, Assembly testAssembly)
        {
            _baseMethodInfo = baseMethodInfo;
            _logger = logger;
            _testAssembly = testAssembly;
        }

        public string DeclaringType 
        { 
            get => _baseMethodInfo.DeclaringType;
            set => _baseMethodInfo.DeclaringType = value;
        }

        public string Name 
        { 
            get => _baseMethodInfo.Name;
            set => _baseMethodInfo.Name = value;
        }

        public string ReflectedType 
        { 
            get => _baseMethodInfo.ReflectedType;
            set => _baseMethodInfo.ReflectedType = value;
        }

        public List<ObjectInfo.Models.TypeInfo.ITypeInfo> CustomAttrs 
        { 
            get => _baseMethodInfo.CustomAttrs;
            set => _baseMethodInfo.CustomAttrs = value;
        }

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
