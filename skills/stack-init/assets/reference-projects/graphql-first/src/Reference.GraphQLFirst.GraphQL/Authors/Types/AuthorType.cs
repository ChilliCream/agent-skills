using System.Security.Claims;
using GreenDonut.Data;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using HotChocolate.Types.Relay;
using Mocha.Mediator;
using Reference.GraphQLFirst.Application.Authors.Queries;
using Reference.GraphQLFirst.Application.Books.Queries;
using Reference.GraphQLFirst.Domain.Authors;
using Reference.GraphQLFirst.Domain.Books;

namespace Reference.GraphQLFirst.GraphQL.Authors.Types;

[ObjectType<Author>]
public static partial class AuthorType
{
    [NodeResolver]
    public static async ValueTask<Author?> GetAuthorByIdAsync(
        Guid id,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
        => await sender.QueryAsync(new GetAuthorByIdQuery(user, id), cancellationToken);

    [UsePaging(ConnectionName = "AuthorBooks")]
    public static async Task<IReadOnlyList<Book>?> GetBooksAsync(
        [Parent] Author author,
        ClaimsPrincipal user,
        ISender sender,
        PagingArguments arguments,
        CancellationToken cancellationToken)
    {
        var page = await sender.QueryAsync(
            new PageBooksByAuthorIdQuery(user, author.Id, arguments),
            cancellationToken);

        return page?.Items.ToArray();
    }
}
