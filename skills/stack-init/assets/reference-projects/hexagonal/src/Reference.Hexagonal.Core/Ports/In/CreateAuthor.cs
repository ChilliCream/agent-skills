using Reference.Hexagonal.Core.Domain;

namespace Reference.Hexagonal.Core.Ports.In;

public sealed record CreateAuthorInput(string Name, string? Biography);

public interface ICreateAuthor
{
    ValueTask<Author> HandleAsync(CreateAuthorInput input, CancellationToken cancellationToken);
}
