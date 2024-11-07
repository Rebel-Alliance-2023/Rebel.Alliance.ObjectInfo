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
    public class PropertyAnalysisTests : IClassFixture<CompilationFixture>
    {
        private readonly CompilationFixture _fixture;

        public PropertyAnalysisTests(CompilationFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void AnalyzeProperties_ShouldIdentifySimpleProperties()
        {
            // Arrange
            var source = @"
                using ObjectInfo.Deepdive.SpecificationGenerator.Attributes;
                public class TestEntity
                {
                    public int Id { get; set; }
                    public string Name { get; set; }
                    public bool IsActive { get; set; }
                }";

            var (typeSymbol, _) = GetTypeSymbol(source);

            // Act 
            var properties = AnalyzeProperties(typeSymbol);

            // Assert
            properties.Should().HaveCount(3);
            properties.Should().Contain(p => p.Symbol.Name == "Id" && p.Symbol.Type.SpecialType == SpecialType.System_Int32);
            properties.Should().Contain(p => p.Symbol.Name == "Name" && p.Symbol.Type.SpecialType == SpecialType.System_String);
            properties.Should().Contain(p => p.Symbol.Name == "IsActive" && p.Symbol.Type.SpecialType == SpecialType.System_Boolean);
        }

        [Fact]
        public void AnalyzeProperties_ShouldRespectIgnoreAttribute()
        {
            // Arrange
            var source = @"
                using ObjectInfo.Deepdive.SpecificationGenerator.Attributes;
                public class TestEntity
                {
                    public int Id { get; set; }
                    [SpecificationProperty(Ignore = true)]
                    public string InternalData { get; set; }
                }";

            var (typeSymbol, _) = GetTypeSymbol(source);

            // Act
            var properties = AnalyzeProperties(typeSymbol);

            // Assert
            properties.Should().HaveCount(1);
            properties.Should().NotContain(p => p.Symbol.Name == "InternalData");
        }

        [Fact]
        public void AnalyzeProperties_ShouldHandleEnumProperties()
        {
            // Arrange
            var source = @"
                using ObjectInfo.Deepdive.SpecificationGenerator.Attributes;
                public enum TestStatus { Active, Inactive }
                public class TestEntity
                {
                    public TestStatus Status { get; set; }
                }";

            var (typeSymbol, _) = GetTypeSymbol(source);

            // Act
            var properties = AnalyzeProperties(typeSymbol);

            // Assert
            properties.Should().ContainSingle();
            var enumProperty = properties.First();
            enumProperty.Symbol.Type.TypeKind.Should().Be(TypeKind.Enum);
            enumProperty.Configuration.GenerateRange.Should().BeFalse();
        }

        [Fact]
        public void AnalyzeProperties_ShouldHandleCollectionProperties()
        {
            // Arrange
            var source = @"
                using System.Collections.Generic;
                using ObjectInfo.Deepdive.SpecificationGenerator.Attributes;
                public class TestEntity
                {
                    public List<string> Tags { get; set; }
                    public int[] Numbers { get; set; }
                }";

            var (typeSymbol, _) = GetTypeSymbol(source);

            // Act
            var properties = AnalyzeProperties(typeSymbol);

            // Assert
            properties.Should().HaveCount(2);
            properties.Should().OnlyContain(p => p.Configuration.GenerateContains == false);
            properties.Should().OnlyContain(p => p.Configuration.GenerateRange == false);
        }

        [Fact]
        public void AnalyzeProperties_ShouldHandleReadOnlyProperties()
        {
            // Arrange
            var source = @"
                using ObjectInfo.Deepdive.SpecificationGenerator.Attributes;
                public class TestEntity
                {
                    public string Name { get; private set; }
                    public int Count { get; }
                }";

            var (typeSymbol, _) = GetTypeSymbol(source);

            // Act
            var properties = AnalyzeProperties(typeSymbol);

            // Assert
            properties.Should().HaveCount(2);
            properties.All(p => !p.Symbol.IsWriteOnly).Should().BeTrue();
        }

        [Fact]
        public void AnalyzeProperties_ShouldHandleComplexTypeProperties()
        {
            // Arrange
            var source = @"
                using ObjectInfo.Deepdive.SpecificationGenerator.Attributes;
                public class Address 
                {
                    public string Street { get; set; }
                }
                public class TestEntity
                {
                    public Address Location { get; set; }
                }";

            var (typeSymbol, _) = GetTypeSymbol(source);

            // Act
            var properties = AnalyzeProperties(typeSymbol);

            // Assert
            var complexProperty = properties.Should().ContainSingle().Subject;
            complexProperty.Symbol.Type.TypeKind.Should().Be(TypeKind.Class);
            complexProperty.Configuration.GenerateContains.Should().BeFalse();
            complexProperty.Configuration.GenerateRange.Should().BeFalse();
        }

        [Fact]
        public void AnalyzeProperties_ShouldHandleIndexedProperties()
        {
            // Arrange
            var source = @"
                using ObjectInfo.Deepdive.SpecificationGenerator.Attributes;
                public class TestEntity
                {
                    public string this[int index] 
                    { 
                        get => null; 
                        set { } 
                    }
                    public string Name { get; set; }
                }";

            var (typeSymbol, _) = GetTypeSymbol(source);

            // Act
            var properties = AnalyzeProperties(typeSymbol);

            // Assert
            properties.Should().ContainSingle();
            properties.Should().NotContain(p => p.Symbol.IsIndexer);
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

        private (INamedTypeSymbol typeSymbol, Compilation compilation) GetTypeSymbol(string source)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            var compilation = CreateTestCompilation(syntaxTree);
            var model = compilation.GetSemanticModel(syntaxTree);
            var typeSymbol = model.GetDeclaredSymbol(syntaxTree.GetRoot()
                .DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
                .First()) as INamedTypeSymbol;

            return (typeSymbol!, compilation);
        }

        private List<PropertyDetails> AnalyzeProperties(INamedTypeSymbol typeSymbol)
        {
            return typeSymbol.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => !p.IsStatic && !p.IsIndexer)
                .Select(p => new PropertyDetails(p, AnalyzePropertyConfiguration(p)))
                .ToList();
        }

        private PropertyConfiguration AnalyzePropertyConfiguration(IPropertySymbol property)
        {
            var attr = property.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == nameof(SpecificationPropertyAttribute));

            if (attr?.NamedArguments.Any(na => na.Key == "Ignore" && (bool)na.Value.Value!) == true)
            {
                return new PropertyConfiguration();
            }

            return new PropertyConfiguration
            {
                GenerateContains = ShouldGenerateContains(property),
                GenerateStartsWith = ShouldGenerateStringOperations(property),
                GenerateEndsWith = ShouldGenerateStringOperations(property),
                GenerateRange = IsRangeableType(property.Type),
                GenerateNullChecks = property.NullableAnnotation == NullableAnnotation.Annotated,
                CaseSensitive = GetAttributeBoolValue(attr, nameof(SpecificationPropertyAttribute.CaseSensitive)),
                CustomExpression = GetAttributeStringValue(attr, nameof(SpecificationPropertyAttribute.CustomExpression))
            };
        }

        private bool ShouldGenerateContains(IPropertySymbol property)
        {
            return property.Type.SpecialType == SpecialType.System_String &&
                   !GetAttributeBoolValue(property.GetAttributes()
                    .FirstOrDefault(a => a.AttributeClass?.Name == nameof(SpecificationPropertyAttribute)),
                    "Ignore");
        }

        private bool ShouldGenerateStringOperations(IPropertySymbol property)
        {
            if (property.Type.SpecialType != SpecialType.System_String)
                return false;

            var attr = property.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == nameof(SpecificationPropertyAttribute));

            return !GetAttributeBoolValue(attr, "Ignore");
        }

        private static bool IsRangeableType(ITypeSymbol type)
        {
            return type.SpecialType switch
            {
                SpecialType.System_Int16 or
                SpecialType.System_Int32 or
                SpecialType.System_Int64 or
                SpecialType.System_Single or
                SpecialType.System_Double or
                SpecialType.System_Decimal => true,
                _ => type.Name == nameof(DateTime)
            };
        }

        private static bool GetAttributeBoolValue(AttributeData? attribute, string propertyName)
        {
            return attribute?.NamedArguments
                .FirstOrDefault(na => na.Key == propertyName)
                .Value.Value as bool? ?? false;
        }

        private static string? GetAttributeStringValue(AttributeData? attribute, string propertyName)
        {
            return attribute?.NamedArguments
                .FirstOrDefault(na => na.Key == propertyName)
                .Value.Value as string;
        }
    }
}
