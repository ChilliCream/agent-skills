using System.Security.Claims;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Mocha.Mediator;
using Reference.CleanArchitecture.Application.Authors.Queries;
using Reference.CleanArchitecture.Application.Books.Queries;
using Reference.CleanArchitecture.Domain.Authors;
using Reference.CleanArchitecture.Domain.Books;

namespace Reference.CleanArchitecture.GraphQL.Authors.Types;

[ObjectType<Author>]
public static partial class AuthorType
{
    static partial void Configure(IObjectTypeDescriptor<Author> descriptor)
    {
        descriptor.Ignore(x => x.Name);
        descriptor.Ignore(x => x.Books);
        descriptor.Ignore(x => x.Events);
    }

    [NodeResolver]
    public static async ValueTask<Author?> GetAuthorByIdAsync(
        Guid id,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        return await sender.QueryAsync(
            new GetAuthorByIdQuery(user, id),
            cancellationToken);
    }

    public static string GetName([Parent] Author author) => author.Name.Value;

    public static async ValueTask<IReadOnlyList<Book>?> GetBooksAsync(
        [Parent] Author author,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken cancellationToken)
    {
        return await sender.QueryAsync(
            new GetBooksByAuthorIdQuery(user, author.Id),
            cancellationToken);
    }
}
