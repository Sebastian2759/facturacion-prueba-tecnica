using Application.Dtos;
using Application.UseCases.Invoices.ListInvoices;
using Domain.Base;
using Domain.Contracts.Adapter.Mapper;
using Domain.Contracts.Persistence;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace Test.Invoices;

public sealed class ListInvoicesQueryTests
{
    [Fact]
    public async Task Handle_Page_PassesOwnerAndReturnsMetadataWithoutEntities()
    {
        var invoice = InvoiceTestData.Entity();
        invoice.Number = 12;
        var dto = new InvoiceListDto { Number = 12 };
        var repository = new Mock<IInvoiceRepository>(MockBehavior.Strict);
        repository.Setup(instance => instance.ListOwnedAsync("owner-a", 2, 10, TestContext.Current.CancellationToken))
            .ReturnsAsync(new InvoicePage([invoice], 11));
        var mapper = new Mock<IMapperAdapter>(MockBehavior.Strict);
        mapper.Setup(instance => instance.Map<InvoiceEntity, InvoiceListDto>(invoice)).Returns(dto);
        var query = new ListInvoicesQuery(repository.Object, mapper.Object, new InvoiceTestUser());

        var result = await query.Handle(new ListInvoicesRequest { Page = 2, PageSize = 10 }, TestContext.Current.CancellationToken);

        result.Data.Should().NotBeNull();
        result.Data!.Page.Should().Be(2);
        result.Data.PageSize.Should().Be(10);
        result.Data.TotalCount.Should().Be(11);
        result.Data.Items.Should().ContainSingle().Which.Should().BeSameAs(dto);
        repository.VerifyAll();
    }

    [Fact]
    public async Task Handle_WithoutPermission_RejectsBeforeQuery()
    {
        var repository = new Mock<IInvoiceRepository>(MockBehavior.Strict);
        var query = new ListInvoicesQuery(repository.Object,
            new Mock<IMapperAdapter>(MockBehavior.Strict).Object, new InvoiceTestUser(canEvaluate: false));

        Func<Task> action = () => query.Handle(new ListInvoicesRequest(), TestContext.Current.CancellationToken);

        await action.Should().ThrowAsync<AccessDeniedException>();
        repository.VerifyNoOtherCalls();
    }
}
