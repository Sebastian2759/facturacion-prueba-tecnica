namespace Domain.Contracts.Adapter.Mapper;

public interface IMapperAdapter
{
    TDestination Map<TSource, TDestination>(TSource source);
    IEnumerable<TDestination> Map<TSource, TDestination>(IEnumerable<TSource> source);
}
