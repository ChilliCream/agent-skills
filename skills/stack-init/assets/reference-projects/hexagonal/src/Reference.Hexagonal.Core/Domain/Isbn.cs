namespace Reference.Hexagonal.Core.Domain;

public sealed record Isbn
{
    private Isbn(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Isbn Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("ISBN is required.", nameof(value));
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length is not 10 and not 13)
        {
            throw new ArgumentException("ISBN must contain 10 or 13 digits.", nameof(value));
        }

        return new Isbn(digits);
    }

    public override string ToString() => Value;
}
