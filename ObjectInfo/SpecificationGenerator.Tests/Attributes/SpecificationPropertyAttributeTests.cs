using ObjectInfo.Deepdive.SpecificationGenerator.Attributes;
using Xunit;
using FluentAssertions;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.Attributes
{
    public class SpecificationPropertyAttributeTests
    {
        [Fact]
        public void Constructor_ShouldSetDefaultValues()
        {
            // Arrange & Act
            var attribute = new SpecificationPropertyAttribute();

            // Assert
            attribute.Ignore.Should().BeFalse();
            attribute.GenerateContains.Should().BeTrue();
            attribute.GenerateStartsWith.Should().BeTrue();
            attribute.GenerateEndsWith.Should().BeTrue();
            attribute.CaseSensitive.Should().BeFalse();
            attribute.GenerateRange.Should().BeTrue();
            attribute.CustomExpression.Should().BeNull();
            attribute.GenerateNullChecks.Should().BeTrue();
        }

        [Fact]
        public void Ignore_WhenSetToTrue_ShouldRetainValue()
        {
            // Arrange & Act
            var attribute = new SpecificationPropertyAttribute { Ignore = true };

            // Assert
            attribute.Ignore.Should().BeTrue();
        }

        [Fact]
        public void StringOperations_WhenDisabled_ShouldRetainValues()
        {
            // Arrange & Act
            var attribute = new SpecificationPropertyAttribute
            {
                GenerateContains = false,
                GenerateStartsWith = false,
                GenerateEndsWith = false
            };

            // Assert
            attribute.GenerateContains.Should().BeFalse();
            attribute.GenerateStartsWith.Should().BeFalse();
            attribute.GenerateEndsWith.Should().BeFalse();
        }

        [Fact]
        public void CaseSensitive_WhenSetToTrue_ShouldRetainValue()
        {
            // Arrange & Act
            var attribute = new SpecificationPropertyAttribute { CaseSensitive = true };

            // Assert
            attribute.CaseSensitive.Should().BeTrue();
        }

        [Fact]
        public void GenerateRange_WhenSetToFalse_ShouldRetainValue()
        {
            // Arrange & Act
            var attribute = new SpecificationPropertyAttribute { GenerateRange = false };

            // Assert
            attribute.GenerateRange.Should().BeFalse();
        }

        [Fact]
        public void CustomExpression_WhenSet_ShouldRetainValue()
        {
            // Arrange
            const string expression = "LOWER({property}) LIKE LOWER({value})";

            // Act
            var attribute = new SpecificationPropertyAttribute { CustomExpression = expression };

            // Assert
            attribute.CustomExpression.Should().Be(expression);
        }

        [Fact]
        public void GenerateNullChecks_WhenSetToFalse_ShouldRetainValue()
        {
            // Arrange & Act
            var attribute = new SpecificationPropertyAttribute { GenerateNullChecks = false };

            // Assert
            attribute.GenerateNullChecks.Should().BeFalse();
        }

        [Fact]
        public void AllProperties_WhenModified_ShouldBeIndependent()
        {
            // Arrange & Act
            var attribute1 = new SpecificationPropertyAttribute
            {
                Ignore = true,
                GenerateContains = false,
                CaseSensitive = true
            };

            var attribute2 = new SpecificationPropertyAttribute();

            // Assert
            attribute1.Ignore.Should().BeTrue();
            attribute1.GenerateContains.Should().BeFalse();
            attribute1.CaseSensitive.Should().BeTrue();

            attribute2.Ignore.Should().BeFalse();
            attribute2.GenerateContains.Should().BeTrue();
            attribute2.CaseSensitive.Should().BeFalse();
        }
    }
}
