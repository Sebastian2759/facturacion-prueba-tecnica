using Application.Base;
using Application.Dtos;
using Domain.Base.Interface;
using Domain.Contracts.Adapter.Mapper;
using Domain.Entities;
using MediatR;

namespace Application.UseCases.DemoArchitectures.GetDemoArchitectureById;

/// <summary>
/// La query contiene el handler del caso de uso; no requiere una clase Handler separada.
/// </summary>
public sealed class GetDemoArchitectureByIdQuery(
    IRepositoryGeneric<DemoArchitectureEntity> repository,
    IMapperAdapter mapper)
    : IRequestHandler<GetDemoArchitectureByIdRequest, ResponseBase<GetDemoArchitectureByIdResponse>>
{
    public async Task<ResponseBase<GetDemoArchitectureByIdResponse>> Handle(
        GetDemoArchitectureByIdRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.FindAsync(request.Id, cancellationToken);

        if (entity is null)
        {
            return ResponseBase<GetDemoArchitectureByIdResponse>.NotFound(
                "DemoArchitecture no encontrado.");
        }

        var dto = mapper.Map<DemoArchitectureEntity, DemoArchitectureDto>(entity);

        return ResponseBase<GetDemoArchitectureByIdResponse>.Ok(
            new GetDemoArchitectureByIdResponse(dto));
    }
}
