using Application.Base;
using MediatR;

namespace Application.UseCases.DemoArchitectures.GetDemoArchitectureById;

public sealed class GetDemoArchitectureByIdRequest
    : IRequest<ResponseBase<GetDemoArchitectureByIdResponse>>
{
    public Guid Id { get; init; }
}
