namespace Domain.Contracts.Accounts;

/// <summary>Provider-neutral account exchanged through the integration boundary.</summary>
public sealed record AccountRecord(
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
    string? ExternalCode);
