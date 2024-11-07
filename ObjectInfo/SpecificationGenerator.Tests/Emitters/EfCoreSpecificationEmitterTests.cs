using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ObjectInfo.Deepdive.SpecificationGenerator.Core.Emitters;
using ObjectInfo.Deepdive.SpecificationGenerator.Core.Models;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.TestInfrastructure.TestFixtures;
using Xunit;
using FluentAssertions;
using System.Text.RegularExpressions;
using Xunit.Abstractions;
using SpecificationGenerator.Tests.TestInfrastructure;
using ObjectInfo.Deepdive.SpecificationGenerator.Core;
using Moq;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.Emitters
{
    public class EfCoreSpecificationEmitterTests : IClassFixture<CompilationFixture>
    {
        private readonly CompilationFixture _fixture;
        private readonly ITestOutputHelper _output;

        public EfCoreSpecificationEmitterTests(CompilationFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
        }

        [Fact]
        public void EmitSpecification_ShouldGenerateBasicEntitySpecification()
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
            var emitter = new EfCoreSpecificationEmitter(testContext);

            // Act
            var result = emitter.EmitSpecification(target);

            // Assert
            result.Should().NotBeNullOrWhiteSpace();
            result.Should().Contain("public class TestEntitySpecification");
            result.Should().Contain("BaseSpecification<TestEntity>");
            result.Should().Contain("public string Name");
            VerifyCompilation(result);
        }

        [Fact]
        public void EmitSpecification_ShouldGenerateFilterMethods()
        {
            // Arrange
            var source = @"
                    public class TestEntity
                    {
                        public string Name { get; set; }
                        public int Age { get; set; }
                        public bool IsActive { get; set; }
                    }";

            var target = CreateSpecificationTarget(source);
            var mockContext = new Mock<ISourceProductionContext>();
            var testContext = new TestSourceProductionContext(_output, mockContext.Object);
            var emitter = new EfCoreSpecificationEmitter(testContext);

            // Act
            var result = emitter.EmitSpecification(target);

            // Assert
            result.Should().Contain("WithName");
            result.Should().Contain("WithAge");
            result.Should().Contain("WithIsActive");
            result.Should().Contain("Criteria = entity =>");
            VerifyCompilation(result);
        }

        [Fact]
        public void EmitSpecification_ShouldGenerateStringSpecificMethods()
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
            var emitter = new EfCoreSpecificationEmitter(testContext);

            // Act
            var result = emitter.EmitSpecification(target);

            // Assert
            result.Should().Contain("WithNameContaining");
            result.Should().Contain("WithNameStartingWith");
            result.Should().Contain("WithNameEndingWith");
            result.Should().Contain(".Contains(");
            result.Should().Contain(".StartsWith(");
            result.Should().Contain(".EndsWith(");
            VerifyCompilation(result);
        }

        [Fact]
        public void EmitSpecification_ShouldGenerateRangeMethods()
        {
            // Arrange
            var source = @"
                    using System;
                    public class TestEntity
                    {
                        public int Count { get; set; }
                        public DateTime Date { get; set; }
                        public decimal Price { get; set; }
                    }";

            var target = CreateSpecificationTarget(source);
            var mockContext = new Mock<ISourceProductionContext>();
            var testContext = new TestSourceProductionContext(_output, mockContext.Object);
            var emitter = new EfCoreSpecificationEmitter(testContext);

            // Act
            var result = emitter.EmitSpecification(target);

            // Assert
            result.Should().Contain("WithCountBetween");
            result.Should().Contain("WithDateBetween");
            result.Should().Contain("WithPriceBetween");
            result.Should().Contain("greaterThanOrEqual");
            result.Should().Contain("lessThanOrEqual");
            VerifyCompilation(result);
        }

        [Fact]
        public void EmitSpecification_ShouldGenerateNavigationPropertyMethods()
        {
            // Arrange
            var source = @"
                    public class Related
                    {
                        public int Id { get; set; }
                    }
                    public class TestEntity
                    {
                        public int Id { get; set; }
                        public Related RelatedEntity { get; set; }
                        public List<Related> RelatedItems { get; set; }
                    }";

            var target = CreateSpecificationTarget(source);
            var mockContext = new Mock<ISourceProductionContext>();
            var testContext = new TestSourceProductionContext(_output, mockContext.Object);
            var emitter = new EfCoreSpecificationEmitter(testContext);

            // Act
            var result = emitter.EmitSpecification(target);

            // Assert
            result.Should().Contain("IncludeRelatedEntity");
            result.Should().Contain("IncludeRelatedItems");
            result.Should().Contain("Include(e => e.RelatedEntity)");
            result.Should().Contain("Include(e => e.RelatedItems)");
            VerifyCompilation(result);
        }

        [Fact]
        public void EmitSpecification_ShouldGenerateOrderingMethods()
        {
            // Arrange
            var source = @"
                    public class TestEntity
                    {
                        public string Name { get; set; }
                        public int Order { get; set; }
                    }";

            var target = CreateSpecificationTarget(source);
            var mockContext = new Mock<ISourceProductionContext>();
            var testContext = new TestSourceProductionContext(_output, mockContext.Object);
            var emitter = new EfCoreSpecificationEmitter(testContext);

            // Act
            var result = emitter.EmitSpecification(target);

            // Assert
            result.Should().Contain("OrderByName");
            result.Should().Contain("OrderByNameDescending");
            result.Should().Contain("OrderByOrder");
            result.Should().Contain("OrderByOrderDescending");
            result.Should().Contain("OrderBy(e => e.");
            result.Should().Contain("OrderByDescending(e => e.");
            VerifyCompilation(result);
        }

        [Fact]
        public void EmitSpecification_ShouldGeneratePagingMethods()
        {
            // Arrange
            var source = @"
                    public class TestEntity
                    {
                        public int Id { get; set; }
                    }";

            var target = CreateSpecificationTarget(source);
            var mockContext = new Mock<ISourceProductionContext>();
            var testContext = new TestSourceProductionContext(_output, mockContext.Object);
            var emitter = new EfCoreSpecificationEmitter(testContext);

            // Act
            var result = emitter.EmitSpecification(target);

            // Assert
            result.Should().Contain("ApplyPaging");
            result.Should().Contain("pageIndex");
            result.Should().Contain("pageSize");
            result.Should().Contain("Skip(");
            result.Should().Contain("Take(");
            VerifyCompilation(result);
        }

        [Fact]
        public void EmitSpecification_ShouldGenerateAsyncMethods()
        {
            // Arrange
            var source = @"
                    public class TestEntity
                    {
                        public int Id { get; set; }
                    }";

            var target = CreateSpecificationTarget(source, generateAsync: true);
            var mockContext = new Mock<ISourceProductionContext>();
            var testContext = new TestSourceProductionContext(_output, mockContext.Object);
            var emitter = new EfCoreSpecificationEmitter(testContext);

            // Act
            var result = emitter.EmitSpecification(target);

            // Assert
            result.Should().Contain("ToListAsync");
            result.Should().Contain("FirstOrDefaultAsync");
            result.Should().Contain("CountAsync");
            result.Should().Contain("CancellationToken");
            VerifyCompilation(result);
        }

        [Fact]
        public void EmitSpecification_ShouldRespectNullableReference()
        {
            // Arrange
            var source = @"
                    public class TestEntity
                    {
                        public string? Name { get; set; }
                        public int? Count { get; set; }
                    }";

            var target = CreateSpecificationTarget(source);
            var mockContext = new Mock<ISourceProductionContext>();
            var testContext = new TestSourceProductionContext(_output, mockContext.Object);
            var emitter = new EfCoreSpecificationEmitter(testContext);

            // Act
            var result = emitter.EmitSpecification(target);

            // Assert
            result.Should().Contain("string?");
            result.Should().Contain("int?");
            result.Should().Contain("WithNameNull()");
            result.Should().Contain("WithNameNotNull()");
            VerifyCompilation(result);
        }

        private SpecificationTarget CreateSpecificationTarget(string source, bool generateAsync = true)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            var compilation = _fixture.CreateCompilation(syntaxTree);
            var model = compilation.GetSemanticModel(syntaxTree);
            var typeSymbol = model.GetDeclaredSymbol(syntaxTree.GetRoot()
                .DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
                .First()) as INamedTypeSymbol;

            return new SpecificationTarget(
                typeSymbol!,
                new SpecificationConfiguration
                {
                    GenerateAsyncMethods = generateAsync,
                    GenerateDocumentation = true,
                    GenerateNavigationSpecs = true
                },
                AnalyzeProperties(typeSymbol!),
                AnalyzeNavigationProperties(typeSymbol!),
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

        // Helper methods from previous test classes
        private List<PropertyDetails> AnalyzeProperties(INamedTypeSymbol typeSymbol) =>
            // Implementation from PropertyAnalysisTests
            new List<PropertyDetails>();

        private List<NavigationPropertyDetails> AnalyzeNavigationProperties(INamedTypeSymbol typeSymbol) =>
            // Implementation from NavigationPropertyTests
            new List<NavigationPropertyDetails>();
    }
}
