using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configuration;

public sealed class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLineEntity>
{
    public void Configure(EntityTypeBuilder<InvoiceLineEntity> builder)
    {
        builder.ToTable("DetalleFactura", table =>
        {
            table.HasCheckConstraint("CK_DetalleFactura_Positive", "[Code] > 0 AND [Quantity] > 0 AND [UnitPrice] > 0 AND [LineTotal] > 0");
            table.HasCheckConstraint("CK_DetalleFactura_Position", "[Position] >= 0 AND [Position] < 100");
        });
        builder.HasKey(line => line.Id);
        builder.Property(line => line.Id).ValueGeneratedNever();
        builder.Property(line => line.InvoiceId).IsRequired();
        builder.Property(line => line.Position).IsRequired();
        builder.Property(line => line.Code).HasColumnType("int").IsRequired();
        builder.Property(line => line.Quantity).HasColumnType("int").IsRequired();
        builder.Property(line => line.UnitPrice).HasColumnType("money").IsRequired();
        builder.Property(line => line.LineTotal).HasColumnType("money").IsRequired();
        builder.Property(line => line.CreatedAtUtc).IsRequired();
        builder.Property(line => line.UpdatedAtUtc).IsRequired();
        builder.Property(line => line.IsActive).HasDefaultValue(true);
        builder.HasIndex(line => new { line.InvoiceId, line.Position }).IsUnique();
    }
}
