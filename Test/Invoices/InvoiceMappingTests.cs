using Adapters;
using Application.Dtos;
using Domain.Contracts.Adapter.Mapper;
using Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Test.Invoices;

public sealed class InvoiceMappingTests
{
    [Fact]
    public void Convention_MapsInvoiceAndNestedDetailsWithoutExposingOwnerOrKey()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAdapterServices();
        using var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapperAdapter>();
        var invoice = InvoiceTestData.Entity();
        invoice.Number = 25;

        var detail = mapper.Map<InvoiceEntity, InvoiceDetailDto>(invoice);
        var summary = mapper.Map<InvoiceEntity, InvoiceListDto>(invoice);

        detail.Should().BeEquivalentTo(new
        {
            invoice.Id,
            invoice.Number,
            invoice.Document,
            invoice.FirstName,
            invoice.LastName,
            invoice.Address,
            invoice.Phone,
            invoice.Date,
            invoice.Gross,
            invoice.Discount,
            invoice.Tax,
            invoice.Total,
        });
        detail.Details.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            Code = 10,
            Quantity = 2,
            UnitPrice = 250_000m,
            LineTotal = 500_000m,
        });
        summary.Number.Should().Be(25);
        summary.Total.Should().Be(565_250m);
        typeof(InvoiceDetailDto).GetProperties().Select(property => property.Name)
            .Should().NotContain(["OwnerSubject", "IdempotencyKey", "RequestFingerprint"]);
    }
}
