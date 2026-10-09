using Domain.Base;

namespace Domain.Invoices;

public sealed record InvoiceLineValue(int Code, int Quantity, decimal UnitPrice);
public sealed record InvoiceLineCalculation(int Code, int Quantity, decimal UnitPrice, decimal LineTotal);
public sealed record InvoiceCalculation(
    IReadOnlyList<InvoiceLineCalculation> Details,
    decimal Gross,
    decimal Discount,
    decimal Tax,
    decimal Total);

/// <summary>
/// Pure calculation shared by preview and persistence. MONEY storage is retained,
/// while accepted prices and displayed amounts have at most two decimal places.
/// </summary>
public static class InvoiceCalculator
{
    public static InvoiceCalculation Calculate(IReadOnlyList<InvoiceLineValue> details)
    {
        if (details is null || details.Count is < 1 or > InvoiceRules.MaxDetails)
            throw new DomainException("details", "Debe agregar entre 1 y 100 artículos.");

        var calculated = new List<InvoiceLineCalculation>(details.Count);
        decimal gross = 0;

        for (var index = 0; index < details.Count; index++)
        {
            var line = details[index];
            if (line is null || line.Code <= 0 || line.Quantity <= 0)
                throw new DomainException($"details[{index}]", "El código y la cantidad deben ser enteros positivos.");

            if (line.UnitPrice <= 0 || line.UnitPrice > InvoiceRules.MaximumMoney ||
                decimal.Round(line.UnitPrice, 2, MidpointRounding.AwayFromZero) != line.UnitPrice)
                throw new DomainException($"details[{index}].unitPrice", "El precio debe ser positivo, tener máximo dos decimales y estar dentro del rango permitido.");

            // Validate before multiplication so both decimal overflow and MONEY
            // overflow produce a domain failure instead of a partial database write.
            if (line.UnitPrice > InvoiceRules.MaximumMoney / line.Quantity)
                throw new DomainException($"details[{index}]", "El importe del artículo excede el rango permitido.");

            var lineTotal = Round(line.Quantity * line.UnitPrice);
            if (gross > InvoiceRules.MaximumMoney - lineTotal)
                throw new DomainException("details", "El importe de la factura excede el rango permitido.");

            gross += lineTotal;
            calculated.Add(new InvoiceLineCalculation(line.Code, line.Quantity, line.UnitPrice, lineTotal));
        }

        gross = Round(gross);
        var discount = gross >= InvoiceRules.DiscountThreshold ? Round(gross * 0.05m) : 0m;
        var tax = Round((gross - discount) * 0.19m);
        var total = Round(gross - discount + tax);
        if (total > InvoiceRules.MaximumMoney)
            throw new DomainException("details", "El total de la factura excede el rango permitido.");

        return new InvoiceCalculation(calculated, gross, discount, tax, total);
    }

    private static decimal Round(decimal value)
        => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}
