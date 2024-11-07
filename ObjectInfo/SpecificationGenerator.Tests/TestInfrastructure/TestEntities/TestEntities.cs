namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.TestInfrastructure.TestEntities
{
    public class TestEntity
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public decimal? Value { get; set; }
        public TestEntityStatus Status { get; set; }
        public virtual ComplexEntity? RelatedEntity { get; set; }
        public virtual ICollection<NestedEntity> NestedEntities { get; set; } = new List<NestedEntity>();
        public Dictionary<string, string>? Metadata { get; set; }
    }

    public enum TestEntityStatus
    {
        Draft,
        Active,
        Archived
    }
}
