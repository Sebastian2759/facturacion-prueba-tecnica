namespace Domain.Invoices;

public static class InvoiceRules
{
    public const int MaxDetails = 100;
    public const int DocumentMaxLength = 15;
    public const int NameMaxLength = 100;
    public const int AddressMaxLength = 200;
    public const int PhoneMaxLength = 30;
    public const int OwnerMaxLength = 64;
    // Application cap keeps every two-decimal amount safe to display through JSON/JavaScript numbers.
    // SQL MONEY has a larger storage range; that range is not the accepted application contract.
    public const decimal MaximumMoney = 999_999_999_999.99m;
    public const decimal DiscountThreshold = 500_000m;
}
