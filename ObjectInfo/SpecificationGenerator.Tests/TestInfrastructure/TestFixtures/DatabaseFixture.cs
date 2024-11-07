using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.TestInfrastructure.Helpers;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.TestInfrastructure.TestEntities;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.TestInfrastructure.TestFixtures
{
    public class DatabaseFixture : IDisposable
    {
        public TestDbContext DbContext { get; }
        private readonly ServiceProvider _serviceProvider;
        private readonly TestDataGenerator _dataGenerator;

        public DatabaseFixture()
        {
            var services = new ServiceCollection();
            
            services.AddDbContext<TestDbContext>(options =>
                options.UseInMemoryDatabase("TestDatabase_" + Guid.NewGuid().ToString()));
            
            _serviceProvider = services.BuildServiceProvider();
            DbContext = _serviceProvider.GetRequiredService<TestDbContext>();
            _dataGenerator = new TestDataGenerator(DbContext);

            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            DbContext.Database.EnsureCreated();
            _dataGenerator.SeedTestData();
        }

        public IQueryable<TestEntity> CreateTestData(int count = 10)
        {
            return _dataGenerator.CreateTestEntities(count).AsQueryable();
        }

        public IQueryable<ComplexEntity> CreateComplexData(int count = 5)
        {
            return _dataGenerator.CreateComplexEntities(count).AsQueryable();
        }

        public void ResetDatabase()
        {
            DbContext.Database.EnsureDeleted();
            DbContext.Database.EnsureCreated();
            _dataGenerator.SeedTestData();
        }

        public void Dispose()
        {
            DbContext.Database.EnsureDeleted();
            DbContext.Dispose();
            _serviceProvider.Dispose();
        }

        public class TestDbContext : DbContext
        {
            public DbSet<TestEntity> TestEntities { get; set; }
            public DbSet<ComplexEntity> ComplexEntities { get; set; }
            public DbSet<NestedEntity> NestedEntities { get; set; }

            public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
            {
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);

                modelBuilder.Entity<TestEntity>()
                    .HasMany(e => e.NestedEntities)
                    .WithOne(e => e.Owner)
                    .HasForeignKey(e => e.OwnerId);

                modelBuilder.Entity<ComplexEntity>()
                    .HasMany(e => e.Children)
                    .WithOne(e => e.Parent)
                    .HasForeignKey(e => e.ParentId);

                modelBuilder.Entity<ComplexEntity>()
                    .HasOne(e => e.Parent)
                    .WithMany()
                    .HasForeignKey(e => e.ParentId);

                modelBuilder.Entity<ComplexEntity>()
                    .HasOne(e => e.Details)
                    .WithOne()
                    .HasForeignKey<ComplexEntityDetails>("ComplexEntityId");
            }
        }
    }
}
