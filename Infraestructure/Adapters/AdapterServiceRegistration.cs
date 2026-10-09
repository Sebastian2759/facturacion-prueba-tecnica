using Domain.Base;
using Infraestructure.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Adapters;

public static class AdapterServiceRegistration
{
    public static IServiceCollection AddAdapterServices(this IServiceCollection services)
    {
        // AutoMapper with generic convention profile; no profile per entity is required.
        services.AddAutoMapper(
            _ => { },
            typeof(AdapterServiceRegistration).Assembly);

        // Automatic convention-based registration: Domain interfaces -> Adapters classes.
        DependencyInjectionHelper.AddAssemblyServices(
            services,
            typeof(EntityBase).Assembly,
            typeof(AdapterServiceRegistration).Assembly);

        return services;
    }
}
