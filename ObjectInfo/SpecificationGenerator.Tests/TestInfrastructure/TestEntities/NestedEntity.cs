namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.TestInfrastructure.TestEntities
{
    public class NestedEntity
    {
        public int Id { get; set; }
        public string? Key { get; set; }
        public string? Value { get; set; }
        public int Order { get; set; }
        public DateTime CreatedAt { get; set; }
        public virtual TestEntity? Owner { get; set; }
        public int? OwnerId { get; set; }
        public virtual ComplexEntity? Parent { get; set; }
        public int? ParentId { get; set; }
        public NestedEntityScope Scope { get; set; }
        public List<string>? References { get; set; }
    }

    public enum NestedEntityScope
    {
        Private,
        Protected,
        Public
    }
}
