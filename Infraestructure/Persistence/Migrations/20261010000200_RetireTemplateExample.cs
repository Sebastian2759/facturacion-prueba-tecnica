using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Persistence.Context;

namespace Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261010000200_RetireTemplateExample")]
public sealed class RetireTemplateExample : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // An unexpected populated legacy table must be reviewed, never silently erased.
        migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM dbo.DemoArchitectures) THROW 51001, 'Legacy example table contains data; migration refused.', 1;");
        migrationBuilder.DropTable(name: "DemoArchitectures", schema: "dbo");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
        => migrationBuilder.CreateTable(name: "DemoArchitectures", schema: "dbo", columns: table => new
        {
            Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
            Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
            CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
            UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
            IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
        }, constraints: table => table.PrimaryKey("PK_DemoArchitectures", x => x.Id));

    protected override void BuildTargetModel(ModelBuilder modelBuilder) => AccountsOnlyModel.Build(modelBuilder);
}
