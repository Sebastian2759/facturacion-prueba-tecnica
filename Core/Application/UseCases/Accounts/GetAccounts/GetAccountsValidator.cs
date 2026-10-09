using FluentValidation;

namespace Application.UseCases.Accounts.GetAccounts;

public sealed class GetAccountsValidator : AbstractValidator<GetAccountsRequest>
{
    public GetAccountsValidator()
    {
        RuleFor(request => request.Mode).Must(mode => mode is "simulated" or "real")
            .WithMessage("El modo debe ser simulated o real.");
        RuleFor(request => request.Code).Matches("^[A-Za-z0-9_-]{1,15}$")
            .When(request => request.Code is not null)
            .WithMessage("El código admite entre 1 y 15 letras, números, guiones o guiones bajos.");
    }
}
