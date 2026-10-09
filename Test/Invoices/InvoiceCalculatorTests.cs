using System.Globalization;
using Domain.Base;
using Domain.Invoices;
using FluentAssertions;

namespace Test.Invoices;

public sealed class InvoiceCalculatorTests
{
    [Theory]
    [InlineData("499999.99", "0", "94999.9981", "594999.99")]
    [InlineData("500000", "25000", "90250", "565250")]
    [InlineData("500000.01", "25000", "90250.0019", "565250.01")]
    public void Calculate_DiscountBoundary_UsesDiscountedTaxBase(
        string price, string discount, string taxBeforeRounding, string total)
    {
        static decimal Value(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);
        var result = InvoiceCalculator.Calculate([new InvoiceLineValue(1, 1, Value(price))]);

        result.Gross.Should().Be(Value(price));
        result.Discount.Should().Be(Value(discount));
        result.Tax.Should().Be(decimal.Round(Value(taxBeforeRounding), 2, MidpointRounding.AwayFromZero));
        result.Total.Should().Be(Value(total));
    }

    [Fact]
    public void Calculate_MultipleLines_RoundsTaxAwayFromZero()
    {
        var result = InvoiceCalculator.Calculate([
            new InvoiceLineValue(1, 1, 0.25m),
            new InvoiceLineValue(2, 3, 0.25m),
            new InvoiceLineValue(3, 1, 0.50m)]);

        result.Gross.Should().Be(1.50m);
        result.Tax.Should().Be(0.29m);
        result.Total.Should().Be(1.79m);
        result.Details.Sum(line => line.LineTotal).Should().Be(result.Gross);
    }

    [Theory]
    [InlineData(0, 1, "1")]
    [InlineData(1, 0, "1")]
    [InlineData(1, -1, "1")]
    [InlineData(1, 1, "0")]
    [InlineData(1, 1, "-1")]
    [InlineData(1, 1, "0.001")]
    [InlineData(1, 1, "1000000000000")]
    [InlineData(1, int.MaxValue, "999999999999.99")]
    public void Calculate_InvalidLine_RejectsWithoutOverflow(int code, int quantity, string price)
    {
        Action calculate = () => InvoiceCalculator.Calculate([
            new InvoiceLineValue(code, quantity, decimal.Parse(price, CultureInfo.InvariantCulture))]);

        calculate.Should().Throw<DomainException>();
    }

    [Fact]
    public void Calculate_TotalExceedsApplicationCap_RejectsBeforePersistence()
    {
        Action calculate = () => InvoiceCalculator.Calculate([
            new InvoiceLineValue(1, 1, InvoiceRules.MaximumMoney)]);

        calculate.Should().Throw<DomainException>();
    }

    [Fact]
    public void Calculate_PreviouslyAcceptedUnsafeJsonAmount_RejectsBeforePersistence()
    {
        // This price is valid on its own, but its quantity previously produced a total
        // whose cents changed when the JSON decimal was parsed as a JavaScript number.
        Action calculate = () => InvoiceCalculator.Calculate([
            new InvoiceLineValue(1, 1000, 800_000_000_000.01m)]);

        calculate.Should().Throw<DomainException>();
    }

    [Fact]
    public void Calculate_CombinedLinesExceedApplicationCap_RejectsBeforePersistence()
    {
        Action calculate = () => InvoiceCalculator.Calculate([
            new InvoiceLineValue(1, 1, 600_000_000_000m),
            new InvoiceLineValue(2, 1, 600_000_000_000m)]);

        calculate.Should().Throw<DomainException>();
    }

    [Fact]
    public void Calculate_LargeAcceptedAmount_PreservesCentsThroughJsonDouble()
    {
        var result = InvoiceCalculator.Calculate([
            new InvoiceLineValue(1, 1, 800_000_000_000.01m)]);

        result.Total.Should().Be(904_400_000_000.01m);
        decimal[] amounts = [result.Gross, result.Discount, result.Tax, result.Total,
            result.Details[0].UnitPrice, result.Details[0].LineTotal];
        foreach (var amount in amounts)
        {
            var browserNumber = System.Text.Json.JsonSerializer.Deserialize<double>(
                System.Text.Json.JsonSerializer.Serialize(amount));
            browserNumber.ToString("F2", CultureInfo.InvariantCulture)
                .Should().Be(amount.ToString("F2", CultureInfo.InvariantCulture));
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Calculate_OutsideDetailLimit_Rejects(int count)
    {
        Action calculate = () => InvoiceCalculator.Calculate(
            Enumerable.Repeat(new InvoiceLineValue(1, 1, 1m), count).ToArray());

        calculate.Should().Throw<DomainException>();
    }
}
