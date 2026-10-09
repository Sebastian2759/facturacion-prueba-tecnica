using Api.Handlers;
using Api.Security;
using Application.Base;
using Application.UseCases.Invoices.CreateInvoice;
using Application.UseCases.Invoices.GetInvoiceByNumber;
using Application.UseCases.Invoices.ListInvoices;
using Application.UseCases.Invoices.PreviewInvoice;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.v1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/invoices")]
[Authorize(Policy = Permissions.Evaluate)]
public sealed class InvoicesController(IMediator mediator) : ApiControllerBase
{
    [HttpPost("preview")]
    [ProducesResponseType(typeof(ResponseBase<PreviewInvoiceResponse>), 200)]
    public async Task<IActionResult> Preview([FromBody] PreviewInvoiceRequest request, CancellationToken ct)
        => ToResult(await mediator.Send(request, ct));

    [HttpPost]
    [ProducesResponseType(typeof(ResponseBase<CreateInvoiceResponse>), 201)]
    [ProducesResponseType(typeof(ResponseBase<CreateInvoiceResponse>), 200)]
    public async Task<IActionResult> Create([FromBody] CreateInvoiceRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? key, CancellationToken ct)
    {
        if (!Guid.TryParseExact(key, "D", out var parsed) || parsed == Guid.Empty)
            return BadRequest(ApiProblems.Create(HttpContext, 400, "Se requiere Idempotency-Key con un UUID válido.", "idempotency.required"));
        return ToResult(await mediator.Send(request with { IdempotencyKey = parsed }, ct));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ResponseBase<ListInvoicesResponse>), 200)]
    public async Task<IActionResult> List([FromQuery] ListInvoicesRequest request, CancellationToken ct)
        => ToResult(await mediator.Send(request, ct));

    [HttpGet("{number:int}")]
    [ProducesResponseType(typeof(ResponseBase<GetInvoiceByNumberResponse>), 200)]
    [ProducesResponseType(typeof(ResponseBase<GetInvoiceByNumberResponse>), 404)]
    public async Task<IActionResult> Get(int number, CancellationToken ct)
        => ToResult(await mediator.Send(new GetInvoiceByNumberRequest { Number = number }, ct));
}
