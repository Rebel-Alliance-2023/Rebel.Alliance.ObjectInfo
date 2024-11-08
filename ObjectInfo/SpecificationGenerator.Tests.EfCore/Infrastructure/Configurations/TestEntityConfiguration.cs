using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ObjectInfo.Deepdive.SpecificationGenerator.Tests.EfCore.Models;

namespace ObjectInfo.Deepdive.SpecificationGenerator.Tests.EfCore.Infrastructure.Configurations
{
    public class TestEntityConfiguration : IEntityTypeConfiguration<TestEntity>
    {
        public void Configure(EntityTypeBuilder<TestEntity> builder)
        {
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
            builder.Property(e => e.Value).HasPrecision(18, 2);
            
            builder.HasOne(e => e.RelatedEntity)
                  .WithOne(e => e.TestEntity)
                  .HasForeignKey<RelatedEntity>(e => e.TestEntityId);

            builder.HasMany(e => e.Children)
                  .WithOne(e => e.Parent)
                  .HasForeignKey(e => e.ParentId);
        }
    }

    public class RelatedEntityConfiguration : IEntityTypeConfiguration<RelatedEntity>
    {
        public void Configure(EntityTypeBuilder<RelatedEntity> builder)
        {
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Title).IsRequired().HasMaxLength(200);
            builder.Property(e => e.Price).HasPrecision(18, 2);
        }
    }

    public class ChildEntityConfiguration : IEntityTypeConfiguration<ChildEntity>
    {
        public void Configure(EntityTypeBuilder<ChildEntity> builder)
        {
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        }
    }
}