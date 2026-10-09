using Domain.Invoices;
using FluentValidation;

namespace Application.UseCases.Invoices.Common;

public sealed class InvoiceLineInputValidator : AbstractValidator<InvoiceLineInput>
{
    public InvoiceLineInputValidator()
    {
        RuleFor(line => line.Code).GreaterThan(0)
            .WithMessage("El código debe ser un entero positivo.");
        RuleFor(line => line.Quantity).GreaterThan(0)
            .WithMessage("La cantidad debe ser un entero positivo.");
        RuleFor(line => line.UnitPrice).GreaterThan(0).LessThanOrEqualTo(InvoiceRules.MaximumMoney)
            .WithMessage("El precio debe estar dentro del rango monetario permitido.");
        RuleFor(line => line.UnitPrice)
            .Must(value => decimal.Round(value, 2, MidpointRounding.AwayFromZero) == value)
            .WithMessage("El precio admite máximo dos decimales.");
    }
}
