using FluentValidation;

namespace Application.UseCases.Accounts.CreateAccount;

public sealed class CreateAccountValidator : AbstractValidator<CreateAccountRequest>
{
    public CreateAccountValidator()
    {
        RuleFor(request => request.Code).NotEmpty().Matches("^[A-Za-z0-9_-]{1,15}$");
        RuleFor(request => request.IdentificationType).NotEmpty().Matches("^[A-Za-z0-9]{1,2}$");
        RuleFor(request => request.Document).NotEmpty().Matches("^[A-Za-z0-9-]{1,15}$");
        RuleFor(request => request.Name).NotEmpty().MaximumLength(200).Must(NoControlCharacters);
        RuleFor(request => request.FirstName).MaximumLength(50).Must(NoControlCharacters);
        RuleFor(request => request.LastName).MaximumLength(50).Must(NoControlCharacters);
        RuleFor(request => request.Email).NotEmpty().MaximumLength(100).EmailAddress();
        RuleFor(request => request.PersonType).InclusiveBetween(1, 2);
        RuleFor(request => request.CityCode).NotEmpty().Matches("^[0-9]{1,5}$");
        RuleFor(request => request.DepartmentCode).NotEmpty().Matches("^[0-9]{1,2}$");
        RuleFor(request => request.CountryCode).NotEmpty().Matches("^[0-9]{1,3}$");
        RuleFor(request => request.Address).MaximumLength(100).Must(NoControlCharacters);
        RuleFor(request => request.AdditionalAddress).MaximumLength(100).Must(NoControlCharacters);
        RuleFor(request => request.Phone).MaximumLength(15).Matches("^[0-9+() -]*$");
        RuleFor(request => request.CustomerType).InclusiveBetween(1, 5);
        RuleFor(request => request.Website).MaximumLength(2048).Must(value =>
            string.IsNullOrWhiteSpace(value) ||
            (Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "https" or "http" && string.IsNullOrEmpty(uri.UserInfo)))
            .WithMessage("El sitio web debe ser una URL HTTP o HTTPS válida.");
        RuleFor(request => request.RegisteredAt)
            .InclusiveBetween(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero));
        RuleFor(request => request.ExternalCode).MaximumLength(50).Must(NoControlCharacters);
    }

    private static bool NoControlCharacters(string? value) => value is null || !value.Any(char.IsControl);
}
