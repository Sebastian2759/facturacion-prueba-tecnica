using FluentValidation;

namespace Application.UseCases.Invoices.GetInvoiceByNumber;

public sealed class GetInvoiceByNumberValidator : AbstractValidator<GetInvoiceByNumberRequest>
{
    public GetInvoiceByNumberValidator()
    {
        RuleFor(request => request.Number).GreaterThan(0);
    }
}
