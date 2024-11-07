using ObjectInfo.Deepdive.SpecificationGenerator.Tests.TestInfrastructure.TestEntities;
using Bogus;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.TestInfrastructure.TestFixtures;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.TestInfrastructure.Helpers
{
    public class TestDataGenerator
    {
        private readonly DatabaseFixture.TestDbContext _context;
        private readonly Faker _faker;

        public TestDataGenerator(DatabaseFixture.TestDbContext context)
        {
            _context = context;
            _faker = new Faker();
        }

        public void SeedTestData()
        {
            var testEntities = CreateTestEntities(20);
            _context.TestEntities.AddRange(testEntities);

            var complexEntities = CreateComplexEntities(10);
            _context.ComplexEntities.AddRange(complexEntities);

            var nestedEntities = CreateNestedEntities(30);
            _context.NestedEntities.AddRange(nestedEntities);

            _context.SaveChanges();
        }

        public List<TestEntity> CreateTestEntities(int count)
        {
            var testEntities = new Faker<TestEntity>()
                .RuleFor(e => e.Name, f => f.Company.CompanyName())
                .RuleFor(e => e.IsActive, f => f.Random.Bool())
                .RuleFor(e => e.CreatedDate, f => f.Date.Past())
                .RuleFor(e => e.Value, f => f.Random.Decimal(0, 1000))
                .RuleFor(e => e.Status, f => f.PickRandom<TestEntityStatus>())
                .RuleFor(e => e.Metadata, f => new Dictionary<string, string>
                {
                    { "Key1", f.Random.Word() },
                    { "Key2", f.Random.Word() }
                });

            return testEntities.Generate(count);
        }

        public List<ComplexEntity> CreateComplexEntities(int count)
        {
            var complexEntities = new Faker<ComplexEntity>()
                .RuleFor(e => e.Title, f => f.Commerce.ProductName())
                .RuleFor(e => e.Description, f => f.Lorem.Paragraph())
                .RuleFor(e => e.Type, f => f.PickRandom<ComplexEntityType>())
                .RuleFor(e => e.LastModified, f => f.Date.Recent())
                .RuleFor(e => e.Price, f => f.Random.Decimal(10, 1000))
                .RuleFor(e => e.IsAvailable, f => f.Random.Bool())
                .RuleFor(e => e.Configuration, f => new Dictionary<string, object>
                {
                    { "Setting1", f.Random.Word() },
                    { "Setting2", f.Random.Number(1, 100) }
                })
                .RuleFor(e => e.Details, f => new ComplexEntityDetails
                {
                    Category = f.Commerce.Categories(1)[0],
                    Tags = f.Make(3, () => f.Commerce.ProductAdjective()).ToArray(),
                    Version = f.Random.Number(1, 10)
                });

            return complexEntities.Generate(count);
        }

        public List<NestedEntity> CreateNestedEntities(int count)
        {
            var nestedEntities = new Faker<NestedEntity>()
                .RuleFor(e => e.Key, f => f.Random.Word())
                .RuleFor(e => e.Value, f => f.Random.Words())
                .RuleFor(e => e.Order, f => f.Random.Number(1, 100))
                .RuleFor(e => e.CreatedAt, f => f.Date.Past())
                .RuleFor(e => e.Scope, f => f.PickRandom<NestedEntityScope>())
                .RuleFor(e => e.References, f => f.Make(3, () => f.Random.Guid().ToString()).ToList());

            return nestedEntities.Generate(count);
        }

        public void AssignRelationships()
        {
            var testEntities = _context.TestEntities.ToList();
            var complexEntities = _context.ComplexEntities.ToList();
            var nestedEntities = _context.NestedEntities.ToList();

            foreach (var nested in nestedEntities)
            {
                nested.Owner = _faker.PickRandom(testEntities);
                nested.Parent = _faker.PickRandom(complexEntities);
            }

            foreach (var complex in complexEntities)
            {
                complex.Parent = _faker.PickRandom(testEntities);
            }

            _context.SaveChanges();
        }
    }
}
