using ApexCommerce.Domain.Common;

namespace ApexCommerce.Domain.ValueObjects;

public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency = "USD")
    {
        if (amount < 0)
            throw new ArgumentException("Money amount cannot be negative.", nameof(amount));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency code is required.", nameof(currency));

        Amount = Math.Round(amount, 2);
        Currency = currency.Trim().ToUpperInvariant();
    }

    public static Money Zero(string currency = "USD") => new(0m, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        if (Amount < other.Amount)
            throw new InvalidOperationException("Resulting money amount cannot be negative.");

        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(int quantity)
    {
        if (quantity < 0)
            throw new ArgumentException("Multiplier cannot be negative.", nameof(quantity));

        return new Money(Amount * quantity, Currency);
    }

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot operate on different currencies: {Currency} and {other.Currency}.");
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount:F2} {Currency}";
}

