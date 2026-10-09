using Application.Base;
using Application.Dtos;
using Application.UseCases.Invoices.ListInvoices;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.v1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/demo/invoices")]
public sealed class DemoInvoicesController : ApiControllerBase
{
    [HttpGet, AllowAnonymous]
    [ProducesResponseType(typeof(ResponseBase<ListInvoicesResponse>), 200)]
    public IActionResult Get() => ToResult(ResponseBase<ListInvoicesResponse>.Ok(new ListInvoicesResponse(
    [
        new InvoiceListDto { Number = 1001, Date = new DateTime(2026, 1, 15), Document = "DEMO-001",
            FirstName = "Cliente", LastName = "de ejemplo", Total = 565250m },
        new InvoiceListDto { Number = 1002, Date = new DateTime(2026, 1, 16), Document = "DEMO-002",
            FirstName = "Comprador", LastName = "ficticio", Total = 119000m }
    ], 1, 10, 2), "Ejemplos ficticios de solo lectura; no pertenecen a la base local."));
}
