using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ObjectInfo.Deepdive.SpecificationGenerator.Attributes;
using ObjectInfo.Deepdive.SpecificationGenerator.Core;
using ObjectInfo.Deepdive.SpecificationGenerator.Core.Models;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.TestInfrastructure.TestFixtures;
using Xunit;
using FluentAssertions;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.Core
{
    public class NavigationPropertyTests : IClassFixture<CompilationFixture>
    {
        private readonly CompilationFixture _fixture;

        public NavigationPropertyTests(CompilationFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void AnalyzeNavigationProperties_ShouldIdentifySimpleNavigations()
        {
            // Arrange
            var source = @"
                using ObjectInfo.Deepdive.SpecificationGenerator.Attributes;
                public class Related 
                {
                    public int Id { get; set; }
                }
                public class TestEntity
                {
                    public int Id { get; set; }
                    public Related RelatedEntity { get; set; }
                }";

            var (typeSymbol, _) = GetTypeSymbol(source);

            // Act
            var navProperties = AnalyzeNavigationProperties(typeSymbol);

            // Assert
            navProperties.Should().ContainSingle();
            var navProp = navProperties.First();
            navProp.Symbol.Name.Should().Be("RelatedEntity");
            navProp.IsCollection.Should().BeFalse();
        }

        [Fact]
        public void AnalyzeNavigationProperties_ShouldIdentifyCollections()
        {
            // Arrange
            var source = @"
                using System.Collections.Generic;
                public class Related 
                {
                    public int Id { get; set; }
                }
                public class TestEntity
                {
                    public List<Related> Items { get; set; }
                    public ICollection<Related> MoreItems { get; set; }
                    public Related[] ArrayItems { get; set; }
                }";

            var (typeSymbol, _) = GetTypeSymbol(source);

            // Act
            var navProperties = AnalyzeNavigationProperties(typeSymbol);

            // Assert
            navProperties.Should().HaveCount(3);
            navProperties.Should().OnlyContain(p => p.IsCollection);
        }

        [Fact]
        public void AnalyzeNavigationProperties_ShouldHandleNestedTypes()
        {
            // Arrange
            var source = @"
                public class Container
                {
                    public class Nested
                    {
                        public int Id { get; set; }
                    }

                    public Nested NestedProperty { get; set; }
                }";

            var (typeSymbol, _) = GetTypeSymbol(source);

            // Act
            var navProperties = AnalyzeNavigationProperties(typeSymbol);

            // Assert
            var navProp = navProperties.Should().ContainSingle().Subject;
            navProp.TypeSymbol.Name.Should().Be("Nested");
            navProp.TypeSymbol.ContainingType.Should().NotBeNull();
        }

        [Fact]
        public void AnalyzeNavigationProperties_ShouldHandleRecursiveTypes()
        {
            // Arrange
            var source = @"
                public class TreeNode
                {
                    public int Id { get; set; }
                    public TreeNode Parent { get; set; }
                    public List<TreeNode> Children { get; set; }
                }";

            var (typeSymbol, _) = GetTypeSymbol(source);

            // Act
            var navProperties = AnalyzeNavigationProperties(typeSymbol);

            // Assert
            navProperties.Should().HaveCount(2);
            navProperties.Should().Contain(p => p.Symbol.Name == "Parent" && !p.IsCollection);
            navProperties.Should().Contain(p => p.Symbol.Name == "Children" && p.IsCollection);
        }

        [Fact]
        public void AnalyzeNavigationProperties_ShouldHandleGenericTypes()
        {
            // Arrange
            var source = @"
                public class Entity<T>
                {
                    public int Id { get; set; }
                    public T Value { get; set; }
                }
                public class TestEntity
                {
                    public Entity<string> StringEntity { get; set; }
                    public Entity<int> IntEntity { get; set; }
                }";

            var (typeSymbol, _) = GetTypeSymbol(source);

            // Act
            var navProperties = AnalyzeNavigationProperties(typeSymbol);

            // Assert
            navProperties.Should().HaveCount(2);
            navProperties.Should().OnlyContain(p => p.TypeSymbol.IsGenericType);
        }

        [Fact]
        public void AnalyzeNavigationProperties_ShouldIgnoreStaticProperties()
        {
            // Arrange
            var source = @"
                public class Related 
                {
                    public int Id { get; set; }
                }
                public class TestEntity
                {
                    public static Related StaticProperty { get; set; }
                    public Related NormalProperty { get; set; }
                }";

            var (typeSymbol, _) = GetTypeSymbol(source);

            // Act
            var navProperties = AnalyzeNavigationProperties(typeSymbol);

            // Assert
            navProperties.Should().ContainSingle();
            navProperties.Should().NotContain(p => p.Symbol.IsStatic);
        }

        [Fact]
        public void AnalyzeNavigationProperties_ShouldRespectIgnoreAttribute()
        {
            // Arrange
            var source = @"
                using ObjectInfo.Deepdive.SpecificationGenerator.Attributes;
                public class Related 
                {
                    public int Id { get; set; }
                }
                public class TestEntity
                {
                    [SpecificationProperty(Ignore = true)]
                    public Related IgnoredNavigation { get; set; }
                    public Related IncludedNavigation { get; set; }
                }";

            var (typeSymbol, _) = GetTypeSymbol(source);

            // Act
            var navProperties = AnalyzeNavigationProperties(typeSymbol);

            // Assert
            navProperties.Should().ContainSingle();
            navProperties.Should().NotContain(p => p.Symbol.Name == "IgnoredNavigation");
        }

        private (INamedTypeSymbol typeSymbol, Compilation compilation) GetTypeSymbol(string source)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            var compilation = CreateTestCompilation(syntaxTree);
            var model = compilation.GetSemanticModel(syntaxTree);
            var typeSymbol = model.GetDeclaredSymbol(syntaxTree.GetRoot()
                .DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
                .Last()) as INamedTypeSymbol;

            return (typeSymbol!, compilation);
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

        private bool IsNavigationProperty(IPropertySymbol property)
        {
            // Ignore system types
            if (property.Type.ContainingNamespace?.ToString().StartsWith("System") == true)
                return false;

            // Check if it's marked to be ignored
            var ignoreAttr = property.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == nameof(SpecificationPropertyAttribute));
            if (ignoreAttr?.NamedArguments.Any(na => na.Key == "Ignore" && (bool)na.Value.Value!) == true)
                return false;

            return property.Type.TypeKind == TypeKind.Class || 
                   property.Type.TypeKind == TypeKind.Interface;
        }

        private bool IsCollection(IPropertySymbol property)
        {
            if (property.Type is IArrayTypeSymbol)
                return true;

            var namedType = property.Type as INamedTypeSymbol;
            if (namedType == null)
                return false;

            return namedType.AllInterfaces.Any(i =>
                i.ConstructedFrom.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T);
        }

        private static CSharpCompilation CreateTestCompilation(SyntaxTree syntaxTree)
        {
            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(GenerateSpecificationAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location)
            };

            return CSharpCompilation.Create(
                "TestAssembly",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }
    }
}
