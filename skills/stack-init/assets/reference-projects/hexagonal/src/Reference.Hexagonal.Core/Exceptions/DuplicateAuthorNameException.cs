namespace Reference.Hexagonal.Core.Exceptions;

public sealed class DuplicateAuthorNameException(string name)
    : Exception($"An author named '{name}' already exists.")
{
    public string Name { get; } = name;
}
