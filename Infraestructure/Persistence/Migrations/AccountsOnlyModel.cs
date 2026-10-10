using Microsoft.EntityFrameworkCore;

namespace Persistence.Migrations;

internal static class AccountsOnlyModel
{
    public static void Build(ModelBuilder modelBuilder)
    {
        InvoiceTextModel.Build(modelBuilder);
        modelBuilder.Ignore("Domain.Entities.DemoArchitectureEntity");
    }
}
