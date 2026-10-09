using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Persistence.Context;

#nullable disable

namespace Persistence.Migrations;

/// <summary>
/// Initial schema authored from the reviewed model. The local application-control
/// policy prevents EF scaffolding; CI verifies this migration against the model.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20261009000100_InitialInvoices")]
public sealed class InitialInvoices : Migration
{
    private static readonly string[] OwnerKeyColumns = ["OwnerSubject", "IdempotencyKey"];
    private static readonly string[] OwnerNumberColumns = ["OwnerSubject", "Number"];
    private static readonly string[] LinePositionColumns = ["InvoiceId", "Position"];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "dbo");

        migrationBuilder.CreateTable(
            name: "DemoArchitectures",
            schema: "dbo",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
            },
            constraints: table => table.PrimaryKey("PK_DemoArchitectures", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Facturas",
            schema: "dbo",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Number = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Date = table.Column<DateTime>(type: "datetime", nullable: false),
                OwnerSubject = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64,
                    nullable: false, collation: "Latin1_General_100_BIN2"),
                IdempotencyKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RequestFingerprint = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true,
                    maxLength: 64, nullable: false),
                Document = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: false),
                FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                Gross = table.Column<decimal>(type: "money", nullable: false),
                Discount = table.Column<decimal>(type: "money", nullable: false),
                Tax = table.Column<decimal>(type: "money", nullable: false),
                Total = table.Column<decimal>(type: "money", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Facturas", x => x.Id);
                table.CheckConstraint("CK_Facturas_Amounts", "[Gross] > 0 AND [Discount] >= 0 AND [Tax] >= 0 AND [Total] > 0");
                table.CheckConstraint("CK_Facturas_Document", "LEN([Document]) > 0");
            });

        migrationBuilder.CreateTable(
            name: "DetalleFactura",
            schema: "dbo",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Position = table.Column<int>(type: "int", nullable: false),
                Code = table.Column<int>(type: "int", nullable: false),
                Quantity = table.Column<int>(type: "int", nullable: false),
                UnitPrice = table.Column<decimal>(type: "money", nullable: false),
                LineTotal = table.Column<decimal>(type: "money", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DetalleFactura", x => x.Id);
                table.ForeignKey("FK_DetalleFactura_Facturas_InvoiceId", x => x.InvoiceId,
                    principalSchema: "dbo", principalTable: "Facturas", principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.CheckConstraint("CK_DetalleFactura_Positive", "[Code] > 0 AND [Quantity] > 0 AND [UnitPrice] > 0 AND [LineTotal] > 0");
                table.CheckConstraint("CK_DetalleFactura_Position", "[Position] >= 0 AND [Position] < 100");
            });

        migrationBuilder.CreateIndex("IX_Facturas_Number", "Facturas", "Number", schema: "dbo", unique: true);
        migrationBuilder.CreateIndex("IX_Facturas_OwnerSubject_IdempotencyKey", "Facturas",
            OwnerKeyColumns, schema: "dbo", unique: true);
        migrationBuilder.CreateIndex("IX_Facturas_OwnerSubject_Number", "Facturas",
            OwnerNumberColumns, schema: "dbo");
        migrationBuilder.CreateIndex("IX_DetalleFactura_InvoiceId_Position", "DetalleFactura",
            LinePositionColumns, schema: "dbo", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "DetalleFactura", schema: "dbo");
        migrationBuilder.DropTable(name: "Facturas", schema: "dbo");
        migrationBuilder.DropTable(name: "DemoArchitectures", schema: "dbo");
    }

    protected override void BuildTargetModel(ModelBuilder modelBuilder)
        => InitialInvoicesModel.Build(modelBuilder);
}
