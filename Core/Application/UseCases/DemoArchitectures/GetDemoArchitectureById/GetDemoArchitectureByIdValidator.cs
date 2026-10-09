using FluentValidation;

namespace Application.UseCases.DemoArchitectures.GetDemoArchitectureById;

public sealed class GetDemoArchitectureByIdValidator
    : AbstractValidator<GetDemoArchitectureByIdRequest>
{
    public GetDemoArchitectureByIdValidator()
    {
        RuleFor(request => request.Id)
            .NotEmpty();
    }
}
