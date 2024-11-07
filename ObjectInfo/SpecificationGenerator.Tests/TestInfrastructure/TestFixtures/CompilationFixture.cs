using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using ObjectInfo.Deepdive.SpecificationGenerator.Attributes;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.TestInfrastructure.TestFixtures
{
    public class CompilationFixture : IDisposable
    {
        public AssemblyLoadContext LoadContext { get; }
        private readonly List<string> _temporaryFiles = new();

        public CompilationFixture()
        {
            LoadContext = new AssemblyLoadContext("TestAssembly", isCollectible: true);
        }

        public Assembly CompileAndLoad(string sourceCode, string? assemblyName = null)
        {
            assemblyName ??= $"DynamicAssembly_{Guid.NewGuid():N}";

            var compilation = CreateCompilation(sourceCode, assemblyName);
            var assemblyPath = Path.Combine(Path.GetTempPath(), $"{assemblyName}.dll");
            _temporaryFiles.Add(assemblyPath);

            var emitResult = compilation.Emit(assemblyPath);
            if (!emitResult.Success)
            {
                var errors = string.Join("\n", emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
                throw new InvalidOperationException($"Compilation failed:\n{errors}");
            }

            return LoadContext.LoadFromAssemblyPath(assemblyPath);
        }

        public bool TryCompile(string sourceCode, out Assembly? assembly, out string[] errors)
        {
            try
            {
                assembly = CompileAndLoad(sourceCode);
                errors = Array.Empty<string>();
                return true;
            }
            catch (InvalidOperationException ex)
            {
                assembly = null;
                errors = ex.Message.Split('\n');
                return false;
            }
        }

        public CSharpCompilation CreateCompilation(SyntaxTree syntaxTree, string assemblyName = "TestAssembly")
        {
            return CSharpCompilation.Create(
                assemblyName,
                new[] { syntaxTree },
                GetMetadataReferences(),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                    .WithOptimizationLevel(OptimizationLevel.Debug)
                    .WithPlatform(Platform.AnyCpu)
            );
        }

        public CSharpCompilation CreateCompilation(string sourceCode, string assemblyName = "TestAssembly")
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            return CreateCompilation(syntaxTree, assemblyName);
        }

        private static List<MetadataReference> GetMetadataReferences()
        {
            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(GenerateSpecificationAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Collections").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Linq").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Linq.Expressions").Location)
            };

            var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);
            if (assemblyPath != null)
            {
                references.Add(MetadataReference.CreateFromFile(
                    Path.Combine(assemblyPath, "System.Private.CoreLib.dll")));
            }

            // Add additional references that might be needed for testing
            var additionalAssemblies = new[]
            {
                "System.Text.RegularExpressions",
                "System.Collections.Concurrent",
                "System.Threading",
                "Microsoft.CSharp"
            };

            foreach (var assemblyName in additionalAssemblies)
            {
                try
                {
                    var assembly = Assembly.Load(assemblyName);
                    references.Add(MetadataReference.CreateFromFile(assembly.Location));
                }
                catch (Exception)
                {
                    // Skip if assembly cannot be loaded
                }
            }

            return references;
        }

        public void Dispose()
        {
            LoadContext.Unload();
            foreach (var file in _temporaryFiles)
            {
                try
                {
                    if (File.Exists(file))
                        File.Delete(file);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }
}
