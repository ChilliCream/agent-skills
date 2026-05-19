using Reference.VerticalSlice.Domain;

namespace Reference.VerticalSlice.Features.Authors;

public sealed class AuthorNotFoundException(Guid authorId)
    : Exception($"Author '{authorId}' was not found.")
{
    [ID(nameof(Author))]
    public Guid AuthorId { get; } = authorId;
}
