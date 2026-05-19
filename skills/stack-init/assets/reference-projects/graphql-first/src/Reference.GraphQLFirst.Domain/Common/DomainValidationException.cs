namespace Reference.GraphQLFirst.Domain.Common;

public sealed class DomainValidationException(string message) : Exception(message);
