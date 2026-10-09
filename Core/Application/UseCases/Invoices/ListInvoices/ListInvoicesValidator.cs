using FluentValidation;

namespace Application.UseCases.Invoices.ListInvoices;

public sealed class ListInvoicesValidator : AbstractValidator<ListInvoicesRequest>
{
    public ListInvoicesValidator()
    {
        RuleFor(request => request.Page).InclusiveBetween(1, 1_000_000);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 100);
    }
}
