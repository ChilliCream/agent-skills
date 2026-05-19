namespace Reference.Ddd.Catalog.Products;

public sealed record Sku
{
    private Sku()
    {
        Value = string.Empty;
    }

    public Sku(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("SKU is required.", nameof(value));
        }

        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length > 32)
        {
            throw new ArgumentException("SKU cannot exceed 32 characters.", nameof(value));
        }

        Value = normalized;
    }

    public string Value { get; init; }

    public override string ToString() => Value;
}
