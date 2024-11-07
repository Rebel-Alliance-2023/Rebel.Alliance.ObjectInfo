using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ObjectInfo.Deepdive.SpecificationGenerator.Attributes;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.TestInfrastructure.TestEntities;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.TestInfrastructure.TestFixtures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Xunit;
using FluentAssertions;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.Core
{
    public class SpecificationGeneratorTests : IClassFixture<CompilationFixture>
    {
        private readonly CompilationFixture _fixture;

        public SpecificationGeneratorTests(CompilationFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void Generator_ShouldCreateSpecification_ForSimpleEntity()
        {
            // Arrange
            var source = @"
                using ObjectInfo.Deepdive.SpecificationGenerator.Attributes;
                namespace Test 
                {
                    [GenerateSpecification]
                    public class SimpleEntity
                    {
                        public int Id { get; set; }
                        public string Name { get; set; }
                    }
                }";

            // Act
            var assembly = CompileAndGenerateSpecs(source);

            // Assert
            assembly.Should().NotBeNull();
            var specType = assembly.GetType("Test.Specifications.SimpleEntitySpecification");
            specType.Should().NotBeNull();
            specType.GetProperties().Should().Contain(p => p.Name == "Name");
        }

        // ... other test methods remain the same ...

        private Assembly CompileAndGenerateSpecs(string source)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            var compilation = CreateTestCompilation(syntaxTree);

            var generator = new ObjectInfo.Deepdive.SpecificationGenerator.Core.SpecificationGenerator();
            var driver = CSharpGeneratorDriver.Create(generator);

            GeneratorDriver result = driver.RunGenerators(compilation);
            var generatedTrees = result.GetRunResult()
                .Results
                .SelectMany(r => r.GeneratedSources)
                .Select(s => s.SourceText.ToString())
                .ToList();

            return _fixture.CompileAndLoad(string.Join(Environment.NewLine, generatedTrees));
        }

        private static CSharpCompilation CreateTestCompilation(SyntaxTree syntaxTree)
        {
            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(GenerateSpecificationAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Runtime.CompilerServices.RuntimeHelpers).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo).Assembly.Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
                MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Collections").Location)
            };

            return CSharpCompilation.Create(
                "TestAssembly",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                    .WithNullableContextOptions(NullableContextOptions.Enable));
        }
    }

    internal static class TypeExtensions
    {
        public static string GetDocumentationXml(this Type type)
        {
            // This is a placeholder - implement actual XML documentation retrieval
            return "<summary>Test documentation</summary>";
        }
    }
}
