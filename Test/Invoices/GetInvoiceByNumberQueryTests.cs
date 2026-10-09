using System.Net;
using Application.Dtos;
using Application.UseCases.Invoices.GetInvoiceByNumber;
using Domain.Base;
using Domain.Contracts.Adapter.Mapper;
using Domain.Contracts.Persistence;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace Test.Invoices;

public sealed class GetInvoiceByNumberQueryTests
{
    [Fact]
    public async Task Handle_OwnInvoice_UsesTrustedOwnerAndMapsDto()
    {
        var invoice = InvoiceTestData.Entity();
        invoice.Number = 17;
        var dto = new InvoiceDetailDto { Number = 17 };
        var repository = new Mock<IInvoiceRepository>(MockBehavior.Strict);
        repository.Setup(instance => instance.FindOwnedByNumberAsync(
            "owner-a", 17, TestContext.Current.CancellationToken)).ReturnsAsync(invoice);
        var mapper = new Mock<IMapperAdapter>(MockBehavior.Strict);
        mapper.Setup(instance => instance.Map<InvoiceEntity, InvoiceDetailDto>(invoice)).Returns(dto);
        var query = new GetInvoiceByNumberQuery(repository.Object, mapper.Object, new InvoiceTestUser());

        var result = await query.Handle(new GetInvoiceByNumberRequest { Number = 17 }, TestContext.Current.CancellationToken);

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Data!.Invoice.Should().BeSameAs(dto);
        repository.VerifyAll();
    }

    [Fact]
    public async Task Handle_MissingOrForeignInvoice_ReturnsSameNotFound()
    {
        var repository = new Mock<IInvoiceRepository>(MockBehavior.Strict);
        repository.Setup(instance => instance.FindOwnedByNumberAsync(
            "owner-a", 99, TestContext.Current.CancellationToken)).ReturnsAsync((InvoiceEntity?)null);
        var mapper = new Mock<IMapperAdapter>(MockBehavior.Strict);
        var query = new GetInvoiceByNumberQuery(repository.Object, mapper.Object, new InvoiceTestUser());

        var result = await query.Handle(new GetInvoiceByNumberRequest { Number = 99 }, TestContext.Current.CancellationToken);

        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
        result.Data.Should().BeNull();
        mapper.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WithoutPermission_RejectsBeforeQuery()
    {
        var repository = new Mock<IInvoiceRepository>(MockBehavior.Strict);
        var query = new GetInvoiceByNumberQuery(repository.Object,
            new Mock<IMapperAdapter>(MockBehavior.Strict).Object, new InvoiceTestUser(canEvaluate: false));

        Func<Task> action = () => query.Handle(new GetInvoiceByNumberRequest { Number = 1 }, TestContext.Current.CancellationToken);

        await action.Should().ThrowAsync<AccessDeniedException>();
        repository.VerifyNoOtherCalls();
    }
}
