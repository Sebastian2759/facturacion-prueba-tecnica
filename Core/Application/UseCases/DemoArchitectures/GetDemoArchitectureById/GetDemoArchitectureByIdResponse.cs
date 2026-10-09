using Application.Dtos;

namespace Application.UseCases.DemoArchitectures.GetDemoArchitectureById;

public sealed record GetDemoArchitectureByIdResponse(
    DemoArchitectureDto DemoArchitecture);
