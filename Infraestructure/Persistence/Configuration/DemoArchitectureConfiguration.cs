using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configuration;

public sealed class DemoArchitectureConfiguration
    : IEntityTypeConfiguration<DemoArchitectureEntity>
{
    public const string TableName = "DemoArchitectures";
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 1000;

    public void Configure(EntityTypeBuilder<DemoArchitectureEntity> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .ValueGeneratedNever();

        builder.Property(entity => entity.Name)
            .HasMaxLength(NameMaxLength)
            .IsRequired();

        builder.Property(entity => entity.Description)
            .HasMaxLength(DescriptionMaxLength)
            .IsRequired();

        builder.Property(entity => entity.CreatedAtUtc)
            .IsRequired();

        builder.Property(entity => entity.UpdatedAtUtc)
            .IsRequired();

        builder.Property(entity => entity.IsActive)
            .HasDefaultValue(true)
            .IsRequired();
    }
}
