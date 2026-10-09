using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Persistence.Context;

/// <summary>Scaffolding only; migration generation never opens this connection.</summary>
public sealed class AppDbContextDesignFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost;Database=FacturacionDesignOnly;Integrated Security=true;Encrypt=true;")
            .Options;
        return new AppDbContext(options);
    }
}
