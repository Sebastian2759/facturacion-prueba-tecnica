using Microsoft.EntityFrameworkCore;

namespace Persistence.Migrations;

/// <summary>Historical target model for the lossless VARCHAR correction.</summary>
internal static class InvoiceTextModel
{
    public static void Build(ModelBuilder modelBuilder)
    {
        InitialInvoicesModel.Build(modelBuilder);
        modelBuilder.Entity("Domain.Entities.InvoiceEntity", builder =>
        {
            builder.Property<string>("FirstName").HasMaxLength(400).IsUnicode()
                .HasColumnType("varchar(400)").UseCollation("Latin1_General_100_CI_AS_SC_UTF8");
            builder.Property<string>("LastName").HasMaxLength(400).IsUnicode()
                .HasColumnType("varchar(400)").UseCollation("Latin1_General_100_CI_AS_SC_UTF8");
            builder.Property<string>("Address").HasMaxLength(800).IsUnicode()
                .HasColumnType("varchar(800)").UseCollation("Latin1_General_100_CI_AS_SC_UTF8");
            builder.Property<string>("Phone").HasMaxLength(120).IsUnicode()
                .HasColumnType("varchar(120)").UseCollation("Latin1_General_100_CI_AS_SC_UTF8");
        });
    }
}
