using Microsoft.EntityFrameworkCore;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.EfCore.Models;
using Xunit;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.EfCore.Infrastructure.TestFixtures
{
    public class DatabaseFixture : IAsyncLifetime
    {
        private readonly DbContextOptions<TestDbContext> _options;
        public TestDbContext DbContext { get; private set; }

        public DatabaseFixture()
        {
            _options = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite("DataSource=:memory:")
                .Options;

            DbContext = new TestDbContext(_options);
        }

        public async Task InitializeAsync()
        {
            await DbContext.Database.OpenConnectionAsync();
            await DbContext.Database.EnsureCreatedAsync();
            await SeedTestDataAsync();
        }

        private async Task SeedTestDataAsync()
        {
            var testEntities = Enumerable.Range(1, 10).Select(i => new TestEntity
            {
                Name = $"Test Entity {i}",
                IsActive = i % 2 == 0,
                CreatedDate = DateTime.Today.AddDays(-i),
                Value = i * 100m,
                Status = (TestEntityStatus)(i % 3)
            }).ToList();

            DbContext.TestEntities.AddRange(testEntities);
            await DbContext.SaveChangesAsync();
        }

        public async Task DisposeAsync()
        {
            await DbContext.DisposeAsync();
        }
    }
}