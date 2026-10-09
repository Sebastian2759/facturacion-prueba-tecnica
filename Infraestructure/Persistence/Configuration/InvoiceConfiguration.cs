using Domain.Entities;
using Domain.Invoices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configuration;

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<InvoiceEntity>
{
    public void Configure(EntityTypeBuilder<InvoiceEntity> builder)
    {
        builder.ToTable("Facturas", table =>
        {
            table.HasCheckConstraint("CK_Facturas_Amounts", "[Gross] > 0 AND [Discount] >= 0 AND [Tax] >= 0 AND [Total] > 0");
            table.HasCheckConstraint("CK_Facturas_Document", "LEN([Document]) > 0");
        });
        builder.HasKey(invoice => invoice.Id);
        builder.Property(invoice => invoice.Id).ValueGeneratedNever();
        builder.Property(invoice => invoice.Number).UseIdentityColumn();
        builder.HasIndex(invoice => invoice.Number).IsUnique();
        builder.Property(invoice => invoice.Date).HasColumnType("datetime").IsRequired();
        builder.Property(invoice => invoice.Document).HasColumnType("varchar(15)")
            .HasMaxLength(InvoiceRules.DocumentMaxLength).IsRequired();
        builder.Property(invoice => invoice.FirstName).HasMaxLength(InvoiceRules.NameMaxLength).IsRequired();
        builder.Property(invoice => invoice.LastName).HasMaxLength(InvoiceRules.NameMaxLength).IsRequired();
        builder.Property(invoice => invoice.Address).HasMaxLength(InvoiceRules.AddressMaxLength).IsRequired();
        builder.Property(invoice => invoice.Phone).HasMaxLength(InvoiceRules.PhoneMaxLength).IsRequired();
        builder.Property(invoice => invoice.OwnerSubject).HasMaxLength(InvoiceRules.OwnerMaxLength)
            .IsUnicode(false).UseCollation("Latin1_General_100_BIN2").IsRequired();
        builder.Property(invoice => invoice.IdempotencyKey).IsRequired();
        builder.Property(invoice => invoice.RequestFingerprint).HasMaxLength(64).IsFixedLength()
            .IsUnicode(false).IsRequired();
        builder.HasIndex(invoice => new { invoice.OwnerSubject, invoice.IdempotencyKey }).IsUnique();
        builder.HasIndex(invoice => new { invoice.OwnerSubject, invoice.Number });
        builder.Property(invoice => invoice.Gross).HasColumnType("money");
        builder.Property(invoice => invoice.Discount).HasColumnType("money");
        builder.Property(invoice => invoice.Tax).HasColumnType("money");
        builder.Property(invoice => invoice.Total).HasColumnType("money");
        builder.Property(invoice => invoice.CreatedAtUtc).IsRequired();
        builder.Property(invoice => invoice.UpdatedAtUtc).IsRequired();
        builder.Property(invoice => invoice.IsActive).HasDefaultValue(true);
        builder.HasMany(invoice => invoice.Details).WithOne(line => line.Invoice)
            .HasForeignKey(line => line.InvoiceId).OnDelete(DeleteBehavior.Cascade);
    }
}
