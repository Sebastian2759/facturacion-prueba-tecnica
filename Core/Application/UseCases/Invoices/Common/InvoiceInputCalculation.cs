using Domain.Invoices;

namespace Application.UseCases.Invoices.Common;

internal static class InvoiceInputCalculation
{
    public static InvoiceCalculation Calculate(IReadOnlyList<InvoiceLineInput> details)
        => InvoiceCalculator.Calculate(details?.Select(line =>
            line is null ? null! : new InvoiceLineValue(line.Code, line.Quantity, line.UnitPrice)).ToArray()!);
}
