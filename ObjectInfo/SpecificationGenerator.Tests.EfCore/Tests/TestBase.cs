using Microsoft.EntityFrameworkCore;
using ObjectInfo.Deepdive.SpecificationGenerator.Runtime;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.EfCore.Infrastructure.TestFixtures;
using Xunit;
using Xunit.Abstractions;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.EfCore.Tests
{
    public abstract class TestBase : IClassFixture<DatabaseFixture>
    {
        protected readonly DatabaseFixture Fixture;
        protected readonly ITestOutputHelper Output;

        protected TestBase(DatabaseFixture fixture, ITestOutputHelper output)
        {
            Fixture = fixture;
            Output = output;
        }

        protected async Task<List<T>> ExecuteSpecification<T>(ISpecification<T> spec) where T : class
        {
            return await Fixture.DbContext.Set<T>()
                .Where(spec.Criteria)
                .ToListAsync();
        }
    }
}