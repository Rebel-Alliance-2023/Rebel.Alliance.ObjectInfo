using ObjectInfo.Deepdive.SpecificationGenerator.Attributes;
using Xunit;
using FluentAssertions;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.Attributes
{
    public class GenerateSpecificationAttributeTests
    {
        [Fact]
        public void Constructor_ShouldSetDefaultValues()
        {
            // Arrange & Act
            var attribute = new GenerateSpecificationAttribute();

            // Assert
            attribute.TargetOrm.Should().Be(OrmTarget.EntityFrameworkCore);
            attribute.GenerateNavigationSpecs.Should().BeTrue();
            attribute.BaseClass.Should().BeNull();
            attribute.GenerateDocumentation.Should().BeTrue();
            attribute.GenerateAsyncMethods.Should().BeTrue();
            attribute.TargetNamespace.Should().BeNull();
        }

        [Theory]
        [InlineData(OrmTarget.Dapper)]
        [InlineData(OrmTarget.EntityFrameworkCore)]
        [InlineData(OrmTarget.Both)]
        public void TargetOrm_ShouldAllowAllValidValues(OrmTarget targetOrm)
        {
            // Arrange & Act
            var attribute = new GenerateSpecificationAttribute { TargetOrm = targetOrm };

            // Assert
            attribute.TargetOrm.Should().Be(targetOrm);
        }

        [Fact]
        public void GenerateNavigationSpecs_WhenSetToFalse_ShouldRetainValue()
        {
            // Arrange & Act
            var attribute = new GenerateSpecificationAttribute { GenerateNavigationSpecs = false };

            // Assert
            attribute.GenerateNavigationSpecs.Should().BeFalse();
        }

        [Fact]
        public void BaseClass_WhenSet_ShouldRetainValue()
        {
            // Arrange
            var baseClass = typeof(object);

            // Act
            var attribute = new GenerateSpecificationAttribute { BaseClass = baseClass };

            // Assert
            attribute.BaseClass.Should().Be(baseClass);
        }

        [Fact]
        public void GenerateDocumentation_WhenSetToFalse_ShouldRetainValue()
        {
            // Arrange & Act
            var attribute = new GenerateSpecificationAttribute { GenerateDocumentation = false };

            // Assert
            attribute.GenerateDocumentation.Should().BeFalse();
        }

        [Fact]
        public void GenerateAsyncMethods_WhenSetToFalse_ShouldRetainValue()
        {
            // Arrange & Act
            var attribute = new GenerateSpecificationAttribute { GenerateAsyncMethods = false };

            // Assert
            attribute.GenerateAsyncMethods.Should().BeFalse();
        }

        [Fact]
        public void TargetNamespace_WhenSet_ShouldRetainValue()
        {
            // Arrange
            const string customNamespace = "Custom.Namespace";

            // Act
            var attribute = new GenerateSpecificationAttribute { TargetNamespace = customNamespace };

            // Assert
            attribute.TargetNamespace.Should().Be(customNamespace);
        }
    }
}
