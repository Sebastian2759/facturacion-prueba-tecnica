using System.Net;
using Application.Dtos;
using Application.UseCases.Invoices.CreateInvoice;
using Domain.Base;
using Domain.Contracts.Adapter.Mapper;
using Domain.Contracts.Persistence;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace Test.Invoices;

public sealed class CreateInvoiceCommandTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 2, 0, 0, TimeSpan.Zero);
    private static readonly TimeZoneInfo BusinessZone = TimeZoneInfo.CreateCustomTimeZone(
        "InvoiceTestBusinessZone", TimeSpan.FromHours(-5), "Invoice test", "Invoice test");

    [Fact]
    public async Task Handle_NewInvoice_DerivesOwnerDateTotalsAndDetailsOnServer()
    {
        var request = InvoiceTestData.Request();
        var repository = new Mock<IInvoiceRepository>(MockBehavior.Strict);
        repository.Setup(instance => instance.FindOwnedByIdempotencyKeyAsync(
            "owner-a", request.IdempotencyKey, TestContext.Current.CancellationToken))
            .ReturnsAsync((InvoiceEntity?)null);
        InvoiceEntity? captured = null;
        repository.Setup(instance => instance.CreateOrGetAsync(
            It.IsAny<InvoiceEntity>(), TestContext.Current.CancellationToken))
            .ReturnsAsync((InvoiceEntity invoice, CancellationToken _) =>
            {
                captured = invoice;
                invoice.Number = 73;
                return new InvoiceCreateResult(invoice, true);
            });
        var command = new CreateInvoiceCommand(repository.Object, Mapper().Object,
            new InvoiceTestUser(), new InvoiceTestTimeProvider(Now), BusinessZone);

        var result = await command.Handle(request, TestContext.Current.CancellationToken);

        result.StatusCode.Should().Be(HttpStatusCode.Created);
        result.Data!.Invoice.Number.Should().Be(73);
        captured.Should().NotBeNull();
        captured!.OwnerSubject.Should().Be("owner-a");
        captured.Date.Should().Be(new DateTime(2026, 10, 9));
        captured.CreatedAtUtc.Should().Be(Now.UtcDateTime);
        captured.Total.Should().Be(565_250m);
        captured.RequestFingerprint.Should().HaveLength(64);
        captured.Details.Should().ContainSingle();
        captured.Details.Single().InvoiceId.Should().Be(captured.Id);
        captured.Details.Single().LineTotal.Should().Be(500_000m);
        repository.VerifyAll();
    }

    [Fact]
    public async Task Handle_ReplayAndDifferentPayload_AreIdempotentOrConflict()
    {
        var request = InvoiceTestData.Request();
        InvoiceEntity? stored = null;
        var repository = new Mock<IInvoiceRepository>(MockBehavior.Strict);
        repository.Setup(instance => instance.FindOwnedByIdempotencyKeyAsync(
            "owner-a", request.IdempotencyKey, TestContext.Current.CancellationToken))
            .ReturnsAsync(() => stored);
        repository.Setup(instance => instance.CreateOrGetAsync(
            It.IsAny<InvoiceEntity>(), TestContext.Current.CancellationToken))
            .ReturnsAsync((InvoiceEntity invoice, CancellationToken _) =>
            {
                stored = invoice;
                invoice.Number = 42;
                return new InvoiceCreateResult(invoice, true);
            });
        var command = new CreateInvoiceCommand(repository.Object, Mapper().Object,
            new InvoiceTestUser(), new InvoiceTestTimeProvider(Now), BusinessZone);

        var first = await command.Handle(request, TestContext.Current.CancellationToken);
        var same = await command.Handle(request with
        {
            FirstName = "  Ana  ",
            Details = [request.Details[0] with { UnitPrice = 250_000.00m }],
        }, TestContext.Current.CancellationToken);
        var conflict = await command.Handle(request with { Document = "987654321" }, TestContext.Current.CancellationToken);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        same.StatusCode.Should().Be(HttpStatusCode.OK);
        same.Data!.Invoice.Number.Should().Be(first.Data!.Invoice.Number);
        conflict.StatusCode.Should().Be(HttpStatusCode.Conflict);
        conflict.Data.Should().BeNull();
        repository.Verify(instance => instance.CreateOrGetAsync(
            It.IsAny<InvoiceEntity>(), TestContext.Current.CancellationToken), Times.Once);
    }

    [Fact]
    public async Task Handle_ConcurrentInsertResolvedByRepository_ReturnsExistingInvoice()
    {
        var repository = new Mock<IInvoiceRepository>();
        repository.Setup(instance => instance.CreateOrGetAsync(It.IsAny<InvoiceEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InvoiceEntity invoice, CancellationToken _) =>
            {
                invoice.Number = 51;
                return new InvoiceCreateResult(invoice, false);
            });
        var command = new CreateInvoiceCommand(repository.Object, Mapper().Object,
            new InvoiceTestUser(), new InvoiceTestTimeProvider(Now), BusinessZone);

        var result = await command.Handle(InvoiceTestData.Request(), TestContext.Current.CancellationToken);

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Data!.Invoice.Number.Should().Be(51);
    }

    [Fact]
    public async Task Handle_WithoutPermission_DoesNotReachRepositoryOrMapper()
    {
        var repository = new Mock<IInvoiceRepository>(MockBehavior.Strict);
        var mapper = new Mock<IMapperAdapter>(MockBehavior.Strict);
        var command = new CreateInvoiceCommand(repository.Object, mapper.Object,
            new InvoiceTestUser(canEvaluate: false), new InvoiceTestTimeProvider(Now), BusinessZone);

        Func<Task> action = () => command.Handle(InvoiceTestData.Request(), TestContext.Current.CancellationToken);

        await action.Should().ThrowAsync<AccessDeniedException>();
        repository.VerifyNoOtherCalls();
        mapper.VerifyNoOtherCalls();
    }

    private static Mock<IMapperAdapter> Mapper()
    {
        var mapper = new Mock<IMapperAdapter>();
        mapper.Setup(instance => instance.Map<InvoiceEntity, InvoiceDetailDto>(It.IsAny<InvoiceEntity>()))
            .Returns((InvoiceEntity invoice) => new InvoiceDetailDto
            {
                Id = invoice.Id,
                Number = invoice.Number,
                Total = invoice.Total,
            });
        return mapper;
    }
}
