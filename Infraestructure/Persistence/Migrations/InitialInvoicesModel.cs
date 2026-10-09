using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

#nullable disable

namespace Persistence.Migrations;

/// <summary>
/// Immutable historical model for 20261009000100. Uses named entity types and
/// explicit properties, not current entity classes/configurations, so subsequent
/// model changes remain visible to EF migration comparison.
/// </summary>
internal static class InitialInvoicesModel
{
    public static void Build(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("dbo")
            .HasAnnotation("ProductVersion", "10.0.10")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);
        modelBuilder.UseIdentityColumns();

        modelBuilder.Entity("Domain.Entities.DemoArchitectureEntity", builder =>
        {
            BaseProperties(builder);
            builder.Property<string>("Name").IsRequired().HasMaxLength(200).HasColumnType("nvarchar(200)");
            builder.Property<string>("Description").IsRequired().HasMaxLength(1000).HasColumnType("nvarchar(1000)");
            builder.ToTable("DemoArchitectures", "dbo");
        });

        modelBuilder.Entity("Domain.Entities.InvoiceEntity", builder =>
        {
            BaseProperties(builder);
            builder.Property<int>("Number").ValueGeneratedOnAdd().HasColumnType("int").UseIdentityColumn();
            builder.Property<DateTime>("Date").HasColumnType("datetime");
            builder.Property<string>("OwnerSubject").IsRequired().HasMaxLength(64).IsUnicode(false)
                .HasColumnType("varchar(64)").UseCollation("Latin1_General_100_BIN2");
            builder.Property<Guid>("IdempotencyKey").HasColumnType("uniqueidentifier");
            builder.Property<string>("RequestFingerprint").IsRequired().HasMaxLength(64)
                .IsUnicode(false).IsFixedLength().HasColumnType("char(64)");
            builder.Property<string>("Document").IsRequired().HasMaxLength(15).HasColumnType("varchar(15)");
            builder.Property<string>("FirstName").IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            builder.Property<string>("LastName").IsRequired().HasMaxLength(100).HasColumnType("nvarchar(100)");
            builder.Property<string>("Address").IsRequired().HasMaxLength(200).HasColumnType("nvarchar(200)");
            builder.Property<string>("Phone").IsRequired().HasMaxLength(30).HasColumnType("nvarchar(30)");
            builder.Property<decimal>("Gross").HasColumnType("money");
            builder.Property<decimal>("Discount").HasColumnType("money");
            builder.Property<decimal>("Tax").HasColumnType("money");
            builder.Property<decimal>("Total").HasColumnType("money");
            builder.HasIndex("Number").IsUnique();
            builder.HasIndex("OwnerSubject", "IdempotencyKey").IsUnique();
            builder.HasIndex("OwnerSubject", "Number");
            builder.ToTable("Facturas", "dbo", table =>
            {
                table.HasCheckConstraint("CK_Facturas_Amounts", "[Gross] > 0 AND [Discount] >= 0 AND [Tax] >= 0 AND [Total] > 0");
                table.HasCheckConstraint("CK_Facturas_Document", "LEN([Document]) > 0");
            });
        });

        modelBuilder.Entity("Domain.Entities.InvoiceLineEntity", builder =>
        {
            BaseProperties(builder);
            builder.Property<Guid>("InvoiceId").HasColumnType("uniqueidentifier");
            builder.Property<int>("Position").HasColumnType("int");
            builder.Property<int>("Code").HasColumnType("int");
            builder.Property<int>("Quantity").HasColumnType("int");
            builder.Property<decimal>("UnitPrice").HasColumnType("money");
            builder.Property<decimal>("LineTotal").HasColumnType("money");
            builder.HasIndex("InvoiceId", "Position").IsUnique();
            builder.ToTable("DetalleFactura", "dbo", table =>
            {
                table.HasCheckConstraint("CK_DetalleFactura_Positive", "[Code] > 0 AND [Quantity] > 0 AND [UnitPrice] > 0 AND [LineTotal] > 0");
                table.HasCheckConstraint("CK_DetalleFactura_Position", "[Position] >= 0 AND [Position] < 100");
            });
            builder.HasOne("Domain.Entities.InvoiceEntity", "Invoice")
                .WithMany("Details").HasForeignKey("InvoiceId")
                .OnDelete(DeleteBehavior.Cascade).IsRequired();
            builder.Navigation("Invoice");
        });

        modelBuilder.Entity("Domain.Entities.InvoiceEntity").Navigation("Details");
    }

    private static void BaseProperties(EntityTypeBuilder builder)
    {
        builder.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
        builder.Property<DateTime>("CreatedAtUtc").HasColumnType("datetime2");
        builder.Property<DateTime>("UpdatedAtUtc").HasColumnType("datetime2");
        builder.Property<bool>("IsActive").ValueGeneratedOnAdd().HasColumnType("bit").HasDefaultValue(true);
        builder.HasKey("Id");
    }
}
