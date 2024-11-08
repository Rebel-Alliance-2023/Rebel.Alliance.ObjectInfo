using Microsoft.EntityFrameworkCore;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.EfCore.Models;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.EfCore.Infrastructure
{
    public class TestDbContext : DbContext
    {
        public DbSet<TestEntity> TestEntities { get; set; } = null!;
        public DbSet<RelatedEntity> RelatedEntities { get; set; } = null!;
        public DbSet<ChildEntity> ChildEntities { get; set; } = null!;

        public TestDbContext(DbContextOptions<TestDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(TestDbContext).Assembly);
        }
    }
}