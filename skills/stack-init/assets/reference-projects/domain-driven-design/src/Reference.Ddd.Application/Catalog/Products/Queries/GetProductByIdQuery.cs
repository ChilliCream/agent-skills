using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Mocha.Mediator;
using Reference.Ddd.Application.Catalog.Products.DataLoaders;
using Reference.Ddd.Application.Security;
using Reference.Ddd.Catalog.Products;

namespace Reference.Ddd.Application.Catalog.Products.Queries;

public sealed record GetProductByIdQuery(ClaimsPrincipal User, Guid Id) : IQuery<Product?>;

public sealed class GetProductByIdQueryHandler(
    IProductBatchingContext products,
    IAuthorizationService authorization)
    : IQueryHandler<GetProductByIdQuery, Product?>
{
    public async ValueTask<Product?> HandleAsync(
        GetProductByIdQuery query,
        CancellationToken cancellationToken)
    {
        var (user, id) = query;

        if (user.Identity is not { IsAuthenticated: true })
        {
            return null;
        }

        var product = await products.ProductById.LoadAsync(id, cancellationToken);
        if (product is null)
        {
            return null;
        }

        var result = await authorization.AuthorizeAsync(user, product, ReferencePolicies.CatalogRead);
        return result.Succeeded ? product : null;
    }
}
