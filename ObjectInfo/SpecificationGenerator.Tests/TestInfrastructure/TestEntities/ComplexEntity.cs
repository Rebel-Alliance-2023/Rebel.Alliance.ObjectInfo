namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.TestInfrastructure.TestEntities
{
    public class ComplexEntity
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public ComplexEntityType Type { get; set; }
        public DateTime LastModified { get; set; }
        public decimal Price { get; set; }
        public bool IsAvailable { get; set; }
        public virtual TestEntity? Parent { get; set; }
        public int? ParentId { get; set; }
        public virtual ICollection<NestedEntity> Children { get; set; } = new List<NestedEntity>();
        public Dictionary<string, object>? Configuration { get; set; }

        public ComplexEntityDetails? Details { get; set; }
    }

    public enum ComplexEntityType
    {
        Basic,
        Advanced,
        Premium
    }

    public class ComplexEntityDetails
    {
        public string? Category { get; set; }
        public string[]? Tags { get; set; }
        public int Version { get; set; }
    }
}
