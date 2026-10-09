using Domain.Base;
using Domain.Base.Interface;
using Infraestructure.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Base;
using Persistence.Context;

namespace Persistence;

public static class PersistenceServiceRegistration
{
    public static IServiceCollection AddPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("AppDbContext"),
                sql => sql.EnableRetryOnFailure(maxRetryCount: 3)));

        services.AddScoped(
            typeof(IRepositoryGeneric<>),
            typeof(RepositoryGeneric<>));

        // Registro automático por convención (interfaces Domain ↔ clases Persistence)
        DependencyInjectionHelper.AddAssemblyServices(
            services,
            typeof(EntityBase).Assembly,
            typeof(PersistenceServiceRegistration).Assembly,
            ServiceLifetime.Scoped);

        return services;
    }
}
