using ApexCommerce.Domain.Common;

namespace ApexCommerce.Domain.ValueObjects;

public sealed class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string State { get; }
    public string PostalCode { get; }
    public string Country { get; }

    public Address(string street, string city, string state, string postalCode, string country)
    {
        if (string.IsNullOrWhiteSpace(street)) throw new ArgumentException("Street cannot be empty.", nameof(street));
        if (string.IsNullOrWhiteSpace(city)) throw new ArgumentException("City cannot be empty.", nameof(city));
        if (string.IsNullOrWhiteSpace(postalCode)) throw new ArgumentException("PostalCode cannot be empty.", nameof(postalCode));
        if (string.IsNullOrWhiteSpace(country)) throw new ArgumentException("Country cannot be empty.", nameof(country));

        Street = street.Trim();
        City = city.Trim();
        State = state?.Trim() ?? string.Empty;
        PostalCode = postalCode.Trim();
        Country = country.Trim();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return State;
        yield return PostalCode;
        yield return Country;
    }

    public override string ToString() => $"{Street}, {City}, {State} {PostalCode}, {Country}";
}

