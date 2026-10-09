using Application.UseCases.Invoices.Common;
using Domain.Invoices;
using FluentValidation;

namespace Application.UseCases.Invoices.PreviewInvoice;

public sealed class PreviewInvoiceValidator : AbstractValidator<PreviewInvoiceRequest>
{
    public PreviewInvoiceValidator()
    {
        RuleFor(request => request.Details).NotEmpty()
            .WithMessage("Debe agregar al menos un artículo.");
        RuleFor(request => request.Details)
            .Must(details => details is null || details.Count <= InvoiceRules.MaxDetails)
            .WithMessage("Puede agregar máximo 100 artículos.");
        RuleForEach(request => request.Details).NotNull().SetValidator(new InvoiceLineInputValidator());
    }
}
