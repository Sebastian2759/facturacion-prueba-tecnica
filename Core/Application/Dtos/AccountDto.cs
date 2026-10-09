using Domain.Contracts.Accounts;

namespace Application.Dtos;

public sealed record AccountDto(
    string Code,
    string IdentificationType,
    string Document,
    string Name,
    string? FirstName,
    string? LastName,
    string Email,
    int PersonType,
    string CityCode,
    string DepartmentCode,
    string CountryCode,
    string? Address,
    string? AdditionalAddress,
    string? Phone,
    int CustomerType,
    string? Website,
    DateTimeOffset RegisteredAt,
    string? ExternalCode)
{
    // Boundary records are not persistence entities; mapping is explicit and provider-neutral.
    public static AccountDto FromRecord(AccountRecord account) => new(
        account.Code, account.IdentificationType, account.Document, account.Name,
        account.FirstName, account.LastName, account.Email, account.PersonType,
        account.CityCode, account.DepartmentCode, account.CountryCode,
        account.Address, account.AdditionalAddress, account.Phone, account.CustomerType,
        account.Website, account.RegisteredAt, account.ExternalCode);
}
