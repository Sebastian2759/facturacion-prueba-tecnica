using AutoMapper;
using Domain.Contracts.Adapter.Mapper;

namespace Adapters.Mapper;

/// <summary>
/// Implementación del adaptador de mapeo usando AutoMapper.
/// El IMapperAdapter en Domain mantiene la independencia de AutoMapper en las capas internas.
/// </summary>
public sealed class AutoMapperAdapter(IMapper mapper) : IMapperAdapter
{
    public TDestination Map<TSource, TDestination>(TSource source)
        => mapper.Map<TDestination>(source);

    public IEnumerable<TDestination> Map<TSource, TDestination>(IEnumerable<TSource> source)
        => mapper.Map<IEnumerable<TDestination>>(source);
}
