using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Persistence.Context;

namespace Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261010000100_InvoiceVarcharFields")]
public sealed class InvoiceVarcharFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        ConvertToUtf8(migrationBuilder, "FirstName", 100);
        ConvertToUtf8(migrationBuilder, "LastName", 100);
        ConvertToUtf8(migrationBuilder, "Address", 200);
        ConvertToUtf8(migrationBuilder, "Phone", 30);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF EXISTS (SELECT 1 FROM dbo.Facturas WHERE
                DATALENGTH(CONVERT(nvarchar(max), FirstName)) > 200 OR
                DATALENGTH(CONVERT(nvarchar(max), LastName)) > 200 OR
                DATALENGTH(CONVERT(nvarchar(max), Address)) > 400 OR
                DATALENGTH(CONVERT(nvarchar(max), Phone)) > 60)
                THROW 51000, 'Rollback refused: text exceeds the previous schema capacity.', 1;
            """);
        ConvertToUnicode(migrationBuilder, "FirstName", 100);
        ConvertToUnicode(migrationBuilder, "LastName", 100);
        ConvertToUnicode(migrationBuilder, "Address", 200);
        ConvertToUnicode(migrationBuilder, "Phone", 30);
    }

    private static void ConvertToUtf8(MigrationBuilder builder, string name, int characters)
        => builder.AlterColumn<string>(name: name, schema: "dbo", table: "Facturas",
            type: $"varchar({characters * 4})", unicode: true, maxLength: characters * 4,
            nullable: false, collation: "Latin1_General_100_CI_AS_SC_UTF8",
            oldClrType: typeof(string), oldType: $"nvarchar({characters})",
            oldMaxLength: characters);

    private static void ConvertToUnicode(MigrationBuilder builder, string name, int characters)
        => builder.AlterColumn<string>(name: name, schema: "dbo", table: "Facturas",
            type: $"nvarchar({characters})", maxLength: characters, nullable: false,
            oldClrType: typeof(string), oldType: $"varchar({characters * 4})",
            oldUnicode: true, oldMaxLength: characters * 4,
            oldCollation: "Latin1_General_100_CI_AS_SC_UTF8");

    protected override void BuildTargetModel(ModelBuilder modelBuilder)
        => InvoiceTextModel.Build(modelBuilder);
}
