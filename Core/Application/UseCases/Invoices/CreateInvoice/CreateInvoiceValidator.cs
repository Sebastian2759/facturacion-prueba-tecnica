using Application.UseCases.Invoices.Common;
using Domain.Invoices;
using FluentValidation;

namespace Application.UseCases.Invoices.CreateInvoice;

public sealed class CreateInvoiceValidator : AbstractValidator<CreateInvoiceRequest>
{
    public CreateInvoiceValidator()
    {
        RuleFor(request => request.Document).NotEmpty().MaximumLength(InvoiceRules.DocumentMaxLength)
            .WithMessage("El documento es obligatorio y admite máximo 15 caracteres.");
        RuleFor(request => request.FirstName).NotEmpty().MaximumLength(InvoiceRules.NameMaxLength);
        RuleFor(request => request.LastName).NotEmpty().MaximumLength(InvoiceRules.NameMaxLength);
        RuleFor(request => request.Address).NotEmpty().MaximumLength(InvoiceRules.AddressMaxLength);
        RuleFor(request => request.Phone).NotEmpty().MaximumLength(InvoiceRules.PhoneMaxLength);
        RuleFor(request => request.IdempotencyKey).NotEmpty()
            .WithMessage("Debe proporcionar una cabecera Idempotency-Key UUID válida.");
        RuleFor(request => request.Details).NotEmpty()
            .WithMessage("Debe agregar al menos un artículo.");
        RuleFor(request => request.Details)
            .Must(details => details is null || details.Count <= InvoiceRules.MaxDetails)
            .WithMessage("Puede agregar máximo 100 artículos.");
        RuleForEach(request => request.Details).NotNull().SetValidator(new InvoiceLineInputValidator());
    }
}
