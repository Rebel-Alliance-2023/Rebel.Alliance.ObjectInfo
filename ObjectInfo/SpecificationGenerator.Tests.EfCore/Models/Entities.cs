using System.ComponentModel.DataAnnotations;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.EfCore.Models
{
    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public decimal? Value { get; set; }
        public TestEntityStatus Status { get; set; }
        public virtual RelatedEntity? RelatedEntity { get; set; }
        public virtual ICollection<ChildEntity> Children { get; set; } = new List<ChildEntity>();
    }

    public class RelatedEntity
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public TestEntityType Type { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public virtual TestEntity TestEntity { get; set; } = null!;
        public int TestEntityId { get; set; }
        public ICollection<ChildEntity> Children { get; set; } = new List<ChildEntity>();
    }

    public class ChildEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }
        public virtual TestEntity Parent { get; set; } = null!;
        public int ParentId { get; set; }
        public ChildEntityScope Scope { get; set; }
    }

    public enum TestEntityStatus
    {
        Draft,
        Active,
        Archived,
        Inactive
    }

    public enum TestEntityType
    {
        Basic,
        Standard,
        Advanced,
        Premium
    }

    public enum ChildEntityScope
    {
        Private,
        Protected,
        Public
    }
}