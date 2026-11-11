using ApexCommerce.Domain.Common;

namespace ApexCommerce.Domain.ValueObjects;

public sealed class Sku : ValueObject
{
    public string Value { get; }

    public Sku(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("SKU value cannot be null or empty.", nameof(value));

        var clean = value.Trim().ToUpperInvariant();
        if (clean.Length < 3 || clean.Length > 50)
            throw new ArgumentException("SKU must be between 3 and 50 characters.", nameof(value));

        Value = clean;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}

