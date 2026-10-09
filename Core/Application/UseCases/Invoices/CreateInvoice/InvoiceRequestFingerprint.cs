using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Application.UseCases.Invoices.CreateInvoice;

internal static class InvoiceRequestFingerprint
{
    public static string Calculate(CreateInvoiceRequest request)
    {
        // Explicit field order and invariant numeric representation make 10 and
        // 10.00 the same request without hashing the key, owner or derived totals.
        var canonical = JsonSerializer.Serialize(new
        {
            request.Document,
            request.FirstName,
            request.LastName,
            request.Address,
            request.Phone,
            Details = request.Details.Select(line => new
            {
                line.Code,
                line.Quantity,
                UnitPrice = line.UnitPrice.ToString("G29", CultureInfo.InvariantCulture),
            }),
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
