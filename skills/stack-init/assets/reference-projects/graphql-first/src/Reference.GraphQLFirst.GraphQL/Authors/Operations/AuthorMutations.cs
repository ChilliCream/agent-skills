using System.Security.Claims;
using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Types;
using Mocha.Mediator;
using Reference.GraphQLFirst.Application.Authors.Commands;
using Reference.GraphQLFirst.Application.Authors.Errors;
using Reference.GraphQLFirst.Domain.Authors;
using Reference.GraphQLFirst.Domain.Common;

namespace Reference.GraphQLFirst.GraphQL.Authors.Operations;

[MutationType]
public static partial class AuthorMutations
{
    [Authorize]
    [Error<DuplicateAuthorNameException>]
    [Error<DomainValidationException>]
    [Error<UnauthorizedAccessException>]
    public static async ValueTask<Author> CreateAuthorAsync(
        ClaimsPrincipal user,
        ISender sender,
        string name,
        CancellationToken cancellationToken)
        => await sender.SendAsync(new CreateAuthorCommand(user, name), cancellationToken);
}
