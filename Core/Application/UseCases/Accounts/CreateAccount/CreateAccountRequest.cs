using Application.Base;
using MediatR;

namespace Application.UseCases.Accounts.CreateAccount;

public sealed class CreateAccountRequest : IRequest<ResponseBase<CreateAccountResponse>>
{
    public string Mode { get; init; } = "simulated";
    public string Code { get; init; } = string.Empty;
    public string IdentificationType { get; init; } = string.Empty;
    public string Document { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string Email { get; init; } = string.Empty;
    public int PersonType { get; init; }
    public string CityCode { get; init; } = string.Empty;
    public string DepartmentCode { get; init; } = string.Empty;
    public string CountryCode { get; init; } = string.Empty;
    public string? Address { get; init; }
    public string? AdditionalAddress { get; init; }
    public string? Phone { get; init; }
    public int CustomerType { get; init; }
    public string? Website { get; init; }
    public DateTimeOffset RegisteredAt { get; init; }
    public string? ExternalCode { get; init; }
}
