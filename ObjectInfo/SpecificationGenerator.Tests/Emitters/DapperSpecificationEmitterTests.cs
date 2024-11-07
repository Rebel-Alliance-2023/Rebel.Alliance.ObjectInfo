using Moq;
using Xunit;
using Xunit.Abstractions;
using ObjectInfo.Deepdive.SpecificationGenerator.Core;
using SpecificationGenerator.Tests.TestInfrastructure;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ObjectInfo.Deepdive.SpecificationGenerator.Attributes;
using ObjectInfo.Deepdive.SpecificationGenerator.Core.Emitters;
using ObjectInfo.Deepdive.SpecificationGenerator.Core.Models;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.TestInfrastructure;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.TestInfrastructure.TestFixtures;
using FluentAssertions;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.Emitters
{
    public class DapperSpecificationEmitterTests : IClassFixture<CompilationFixture>
    {
        private readonly CompilationFixture _fixture;
        private readonly ITestOutputHelper _output;

        public DapperSpecificationEmitterTests(CompilationFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
        }

        [Fact]
        public void TestEmitSpecification()
        {
            // Arrange
            var mockContext = new Mock<ISourceProductionContext>();
            var testContext = new TestSourceProductionContext(_output, mockContext.Object);
            var emitter = new DapperSpecificationEmitter(testContext);
            var target = new SpecificationTarget
            (
                ClassSymbol: null, // Replace with actual INamedTypeSymbol instance
                Configuration: new SpecificationConfiguration(),
                Properties: new List<PropertyDetails>(),
                NavigationProperties: new List<NavigationPropertyDetails>(),
                AssemblyConfiguration: new AssemblyConfiguration()
            );

            // Act
            var result = emitter.EmitSpecification(target);

            // Assert
            Assert.NotNull(result);
            // Additional assertions
        }

        [Fact]
        public void EmitSpecification_ShouldGenerateBasicSqlSpecification()
        {
            // Arrange
            var source = @"
                public class TestEntity
                {
                    public int Id { get; set; }
                    public string Name { get; set; }
                }";

            var target = CreateSpecificationTarget(source);
            var mockContext = new Mock<ISourceProductionContext>();
            var testContext = new TestSourceProductionContext(_output, mockContext.Object);
            var emitter = new DapperSpecificationEmitter(testContext);

            // Act
            var result = emitter.EmitSpecification(target);

            // Assert
            result.Should().NotBeNullOrWhiteSpace();
            result.Should().Contain("public class TestEntitySpecification");
            result.Should().Contain("SqlSpecification<TestEntity>");
            result.Should().Contain("BuildWhereClause");
            VerifyCompilation(result);
        }

        [Fact]
        public void EmitSpecification_ShouldGenerateParameterizedQueries()
        {
            // Arrange
            var source = @"
                public class TestEntity
                {
                    public string Name { get; set; }
                    public int Age { get; set; }
                }";

            var target = CreateSpecificationTarget(source);
            var mockContext = new Mock<ISourceProductionContext>();
            var testContext = new TestSourceProductionContext(_output, mockContext.Object);
            var emitter = new DapperSpecificationEmitter(testContext);

            // Act
            var result = emitter.EmitSpecification(target);

            // Assert
            result.Should().Contain("@Name");
            result.Should().Contain("@Age");
            result.Should().Contain("AddParameter");
            result.Should().Contain("DynamicParameters");
            VerifyCompilation(result);
        }

        [Fact]
        public void EmitSpecification_ShouldGenerateStringFilters()
        {
            // Arrange
            var source = @"
                public class TestEntity
                {
                    public string Name { get; set; }
                }";

            var target = CreateSpecificationTarget(source);
            var mockContext = new Mock<ISourceProductionContext>();
            var testContext = new TestSourceProductionContext(_output, mockContext.Object);
            var emitter = new DapperSpecificationEmitter(testContext);

            // Act
            var result = emitter.EmitSpecification(target);

            // Assert
            result.Should().Contain("LIKE");
            result.Should().Contain("%' + @");
            result.Should().Contain("+ '%'");
            result.Should().Contain("LOWER(");
            VerifyCompilation(result);
        }

        private SpecificationTarget CreateSpecificationTarget(string source, bool generateAsync = true)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            var compilation = _fixture.CreateCompilation(syntaxTree);
            var model = compilation.GetSemanticModel(syntaxTree);

            var classDeclaration = syntaxTree.GetRoot()
                .DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .First();

            var typeSymbol = model.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;

            if (typeSymbol == null)
                throw new InvalidOperationException("Failed to get type symbol from source");

            return new SpecificationTarget(
                typeSymbol,
                new SpecificationConfiguration
                {
                    GenerateAsyncMethods = generateAsync,
                    GenerateDocumentation = true,
                    GenerateNavigationSpecs = true
                },
                AnalyzeProperties(typeSymbol),
                AnalyzeNavigationProperties(typeSymbol),
                new AssemblyConfiguration());
        }

        private void VerifyCompilation(string source)
        {
            var success = _fixture.TryCompile(source, out var assembly, out var errors);
            if (!success)
            {
                _output.WriteLine("Compilation failed:");
                foreach (var error in errors)
                {
                    _output.WriteLine(error);
                }
            }
            success.Should().BeTrue("compilation should succeed");
            assembly.Should().NotBeNull();
        }

        private List<PropertyDetails> AnalyzeProperties(INamedTypeSymbol typeSymbol)
        {
            return typeSymbol.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => !p.IsStatic && !p.IsIndexer)
                .Select(p => new PropertyDetails(p, new PropertyConfiguration
                {
                    GenerateContains = p.Type.SpecialType == SpecialType.System_String,
                    GenerateRange = IsRangeableType(p.Type),
                    GenerateNullChecks = p.NullableAnnotation == NullableAnnotation.Annotated
                }))
                .ToList();
        }

        private List<NavigationPropertyDetails> AnalyzeNavigationProperties(INamedTypeSymbol typeSymbol)
        {
            return typeSymbol.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => !p.IsStatic && !p.IsIndexer && IsNavigationProperty(p))
                .Select(p => new NavigationPropertyDetails(
                    p,
                    (INamedTypeSymbol)p.Type,
                    IsCollection(p),
                    p.NullableAnnotation == NullableAnnotation.Annotated))
                .ToList();
        }

        private static bool IsRangeableType(ITypeSymbol type)
        {
            return type.SpecialType switch
            {
                SpecialType.System_Int32 or
                SpecialType.System_Int64 or
                SpecialType.System_Double or
                SpecialType.System_Decimal => true,
                _ => type.Name == nameof(DateTime)
            };
        }

        private static bool IsNavigationProperty(IPropertySymbol property)
        {
            if (property.Type.ContainingNamespace?.ToString()?.StartsWith("System") == true)
                return false;

            return property.Type.TypeKind == TypeKind.Class ||
                   property.Type.TypeKind == TypeKind.Interface;
        }

        private static bool IsCollection(IPropertySymbol property)
        {
            if (property.Type is IArrayTypeSymbol)
                return true;

            if (property.Type is not INamedTypeSymbol namedType)
                return false;

            return namedType.AllInterfaces.Any(i =>
                i.ConstructedFrom.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T);
        }
    }
}