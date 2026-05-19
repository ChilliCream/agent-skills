using System.Security.Claims;
using GreenDonut.Data;
using Microsoft.AspNetCore.Authorization;
using Mocha.Mediator;
using Reference.Ddd.Application.Catalog.Products.DataLoaders;
using Reference.Ddd.Application.Security;
using Reference.Ddd.Catalog.Products;

namespace Reference.Ddd.Application.Catalog.Products.Queries;

public sealed record PageProductsQuery(
    ClaimsPrincipal User,
    ProductStatus Status,
    PagingArguments Paging)
    : IQuery<Page<Product>?>;

public sealed class PageProductsQueryHandler(
    IProductBatchingContext products,
    IAuthorizationService authorization)
    : IQueryHandler<PageProductsQuery, Page<Product>?>
{
    public async ValueTask<Page<Product>?> HandleAsync(
        PageProductsQuery query,
        CancellationToken cancellationToken)
    {
        var (user, status, paging) = query;

        if (user.Identity is not { IsAuthenticated: true })
        {
            return null;
        }

        var result = await authorization.AuthorizeAsync(user, null, ReferencePolicies.CatalogRead);
        if (!result.Succeeded)
        {
            return null;
        }

        return await products.PageProductsByStatus.With(paging).LoadAsync(status, cancellationToken);
    }
}
