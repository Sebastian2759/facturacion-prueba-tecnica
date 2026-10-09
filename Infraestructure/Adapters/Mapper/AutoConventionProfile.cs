using Application;
using AutoMapper;
using Domain.Base;

namespace Adapters.Mapper;

/// <summary>
/// Perfil generico de AutoMapper por convencion de nombres.
///
/// Regla: toda entidad de Domain se mapea automaticamente
/// hacia cualquier Dto de Application cuyo nombre empiece con el nombre base.
///
/// Ejemplo automatico:
///   CustomerEntity -> CustomerDetailDto, CustomerListDto
///
/// Al agregar una nueva entidad + Dto solo cumple la convencion de nombres;
/// no hace falta tocar este archivo.
/// </summary>
public sealed class AutoConventionProfile : Profile
{
    public AutoConventionProfile()
    {
        var domainTypes = typeof(EntityBase).Assembly
            .GetExportedTypes()
            .Where(t =>
                t.IsClass &&
                !t.IsAbstract &&
                t.Name.EndsWith("Entity", StringComparison.Ordinal))
            .ToList();

        var dtoTypes = typeof(ApplicationServiceRegistration).Assembly
            .GetExportedTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Dto", StringComparison.Ordinal))
            .ToList();

        foreach (var source in domainTypes)
        {
            var baseName = source.Name[..^"Entity".Length];

            var matchingDtos = dtoTypes
                .Where(d => d.Name.StartsWith(baseName, StringComparison.OrdinalIgnoreCase))
                .Where(d => !domainTypes.Any(other =>
                    other != source && other.Name.Length > source.Name.Length &&
                    d.Name.StartsWith(other.Name[..^"Entity".Length], StringComparison.OrdinalIgnoreCase)));

            foreach (var dest in matchingDtos)
            {
                CreateMap(source, dest);
            }
        }
    }
}
