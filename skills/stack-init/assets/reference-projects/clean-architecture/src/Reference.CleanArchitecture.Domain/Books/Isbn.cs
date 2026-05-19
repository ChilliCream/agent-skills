namespace Reference.CleanArchitecture.Domain.Books;

public readonly record struct Isbn
{
    public Isbn(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var normalized = value.Replace("-", string.Empty).Trim();
        if (normalized.Length is not 10 and not 13 || normalized.Any(c => !char.IsDigit(c)))
        {
            throw new ArgumentException("ISBN must contain 10 or 13 digits.", nameof(value));
        }

        Value = normalized;
    }

    public string Value { get; }

    public static Isbn Parse(string value) => new(value);

    public override string ToString() => Value;
}
