namespace Reference.Ddd.Ordering.Orders;

public sealed record ShippingAddress
{
    private ShippingAddress()
    {
        Recipient = string.Empty;
        Line1 = string.Empty;
        City = string.Empty;
        Country = string.Empty;
        PostalCode = string.Empty;
    }

    public ShippingAddress(
        string recipient,
        string line1,
        string? line2,
        string city,
        string country,
        string postalCode)
    {
        Recipient = Required(recipient, nameof(recipient), 120);
        Line1 = Required(line1, nameof(line1), 200);
        Line2 = Optional(line2, nameof(line2), 200);
        City = Required(city, nameof(city), 120);
        Country = Required(country, nameof(country), 2).ToUpperInvariant();
        PostalCode = Required(postalCode, nameof(postalCode), 32);
    }

    public string Recipient { get; init; }

    public string Line1 { get; init; }

    public string? Line2 { get; init; }

    public string City { get; init; }

    public string Country { get; init; }

    public string PostalCode { get; init; }

    private static string Required(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Value cannot exceed {maxLength} characters.", parameterName);
        }

        return normalized;
    }

    private static string? Optional(string? value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Value cannot exceed {maxLength} characters.", parameterName);
        }

        return normalized;
    }
}
