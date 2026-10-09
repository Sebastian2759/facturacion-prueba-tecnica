using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Infraestructure.DependencyInjection;

/// <summary>
/// Registra en el contenedor las clases concretas que implementan contratos
/// definidos en el ensamblado de dominio. Cada capa conserva el control de su
/// ensamblado concreto y del ciclo de vida utilizado.
/// </summary>
public static class DependencyInjectionHelper
{
    public static void AddAssemblyServices(
        IServiceCollection services,
        Assembly domainAssembly,
        Assembly concreteAssembly,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        var domainInterfaces = domainAssembly
            .ExportedTypes
            .Where(type => type.IsInterface)
            .ToHashSet();

        var concreteClasses = concreteAssembly
            .ExportedTypes
            .Where(type =>
                type.IsClass &&
                !type.IsAbstract &&
                !type.IsGenericTypeDefinition);

        foreach (var @class in concreteClasses)
        {
            foreach (var @interface in @class.GetInterfaces())
            {
                if (domainInterfaces.Contains(@interface))
                    services.Add(new ServiceDescriptor(@interface, @class, lifetime));
            }
        }
    }
}
