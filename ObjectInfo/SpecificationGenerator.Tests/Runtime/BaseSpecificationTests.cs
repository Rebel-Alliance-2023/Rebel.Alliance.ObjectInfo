using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.TestInfrastructure;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.TestInfrastructure.TestEntities;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.TestInfrastructure.TestFixtures;
using Xunit;
using FluentAssertions;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.Runtime
{
    public class BaseSpecificationTests : IClassFixture<DatabaseFixture>
    {
        private readonly DatabaseFixture _fixture;

        public BaseSpecificationTests(DatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void Criteria_DefaultsToAllTrue()
        {
            // Arrange
            var spec = new TestSpecification<TestEntity>();

            // Act
            var criteria = spec.Criteria.Compile();

            // Assert
            var testEntity = new TestEntity();
            criteria(testEntity).Should().BeTrue();
        }

        [Fact]
        public void And_CombinesSpecificationsCorrectly()
        {
            // Arrange
            var spec1 = new TestSpecification<TestEntity>();
            var spec2 = new TestSpecification<TestEntity>();
            spec1.SetCriteria(e => e.Id > 0);
            spec2.SetCriteria(e => e.Name != null);

            // Act
            var combinedSpec = spec1.And(spec2);

            // Assert
            var criteria = ((TestSpecification<TestEntity>)combinedSpec).Criteria.Compile();
            criteria(new TestEntity { Id = 1, Name = "Test" }).Should().BeTrue();
            criteria(new TestEntity { Id = 0, Name = "Test" }).Should().BeFalse();
            criteria(new TestEntity { Id = 1, Name = null }).Should().BeFalse();
        }

        [Fact]
        public void Or_CombinesSpecificationsCorrectly()
        {
            // Arrange
            var spec1 = new TestSpecification<TestEntity>();
            var spec2 = new TestSpecification<TestEntity>();
            spec1.SetCriteria(e => e.Id > 0);
            spec2.SetCriteria(e => e.Name == "Test");

            // Act
            var combinedSpec = spec1.Or(spec2);

            // Assert
            var criteria = ((TestSpecification<TestEntity>)combinedSpec).Criteria.Compile();
            criteria(new TestEntity { Id = 1, Name = "Other" }).Should().BeTrue();
            criteria(new TestEntity { Id = 0, Name = "Test" }).Should().BeTrue();
            criteria(new TestEntity { Id = 0, Name = "Other" }).Should().BeFalse();
        }

        [Fact]
        public void Not_InvertsSpecificationCorrectly()
        {
            // Arrange
            var spec = new TestSpecification<TestEntity>();
            spec.SetCriteria(e => e.IsActive);

            // Act
            var notSpec = spec.Not();

            // Assert
            var criteria = ((TestSpecification<TestEntity>)notSpec).Criteria.Compile();
            criteria(new TestEntity { IsActive = true }).Should().BeFalse();
            criteria(new TestEntity { IsActive = false }).Should().BeTrue();
        }

        [Fact]
        public void Includes_WorkAsExpected()
        {
            // Arrange
            var spec = new TestSpecification<TestEntity>();
            spec.AddInclude(e => e.RelatedEntity);

            // Assert
            spec.Includes.Should().ContainSingle();
            spec.Includes.First().ToString().Should().Contain("RelatedEntity");
        }

        [Theory]
        [InlineData(0, 10)]
        [InlineData(10, 5)]
        public void Paging_WorksAsExpected(int skip, int take)
        {
            // Arrange
            var spec = new TestSpecification<TestEntity>();
            spec.ApplyPaging(skip, take);

            // Act
            var query = _fixture.CreateTestData().AsQueryable();
            var result = query.Where(spec.Criteria).Skip(skip).Take(take).ToList();

            // Assert
            result.Count.Should().BeLessThanOrEqualTo(take);
            var totalCount = query.LongCount();
            if(skip < totalCount)
            {
                result.Should().BeEquivalentTo(query.Skip(skip).Take(take));
            }
        }

        private class TestSpecification<T> : BaseSpecification<T> where T : class
        {
            public TestSpecification() : base() { }

            public void SetCriteria(Expression<Func<T, bool>> criteria)
            {
                Criteria = criteria;
            }

            public new void AddInclude(Expression<Func<T, object>> includeExpression)
            {
                base.AddInclude(includeExpression);
            }

            public new void ApplyPaging(int skip, int take)
            {
                base.ApplyPaging(skip, take);
            }

            //protected override void AddIncludes()
            //{
            //    // For testing purposes, no includes by default
            //}
        }
    }
}
