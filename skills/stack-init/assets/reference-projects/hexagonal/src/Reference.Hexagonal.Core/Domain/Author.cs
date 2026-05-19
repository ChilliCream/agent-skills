namespace Reference.Hexagonal.Core.Domain;

public sealed class Author
{
    private Author()
    {
    }

    private Author(Guid id, string name, string? biography)
    {
        Id = id;
        Name = name;
        Biography = biography;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string? Biography { get; private set; }

    public static Author Create(string name, string? biography)
        => new(Guid.CreateVersion7(), NormalizeName(name), NormalizeBiography(biography));

    public void UpdateProfile(string name, string? biography)
    {
        Name = NormalizeName(name);
        Biography = NormalizeBiography(biography);
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Author name is required.", nameof(name));
        }

        var normalized = name.Trim();
        if (normalized.Length > 200)
        {
            throw new ArgumentException("Author name cannot exceed 200 characters.", nameof(name));
        }

        return normalized;
    }

    private static string? NormalizeBiography(string? biography)
    {
        if (string.IsNullOrWhiteSpace(biography))
        {
            return null;
        }

        var normalized = biography.Trim();
        if (normalized.Length > 2000)
        {
            throw new ArgumentException("Biography cannot exceed 2000 characters.", nameof(biography));
        }

        return normalized;
    }
}
