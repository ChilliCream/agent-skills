using System.Security.Claims;
using GreenDonut;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Mocha.Mediator;
using Reference.Ddd.Application.Catalog.Abstractions;
using Reference.Ddd.Application.Catalog.Products.Errors;
using Reference.Ddd.Application.Security;
using Reference.Ddd.Catalog.Products;
using Reference.Ddd.SharedKernel;

namespace Reference.Ddd.Application.Catalog.Products.Commands;

public sealed record ChangeProductPriceCommand(
    ClaimsPrincipal User,
    Guid ProductId,
    decimal Amount,
    string Currency)
    : ICommand<Product>;

public sealed class ChangeProductPriceCommandHandler(
    ICatalogDbContext context,
    IPromiseCache cache,
    IAuthorizationService authorization)
    : ICommandHandler<ChangeProductPriceCommand, Product>
{
    public async ValueTask<Product> HandleAsync(
        ChangeProductPriceCommand command,
        CancellationToken cancellationToken)
    {
        var (user, productId, amount, currency) = command;

        if (user.Identity is not { IsAuthenticated: true })
        {
            throw new UnauthorizedAccessException();
        }

        var product = await context.Products.FirstOrDefaultAsync(x => x.Id == productId, cancellationToken);
        if (product is null)
        {
            throw new ProductNotFoundException(productId);
        }

        cache.Publish(product);

        var result = await authorization.AuthorizeAsync(user, product, ReferencePolicies.CatalogManage);
        if (!result.Succeeded)
        {
            throw new ProductNotFoundException(productId);
        }

        product.ChangePrice(new Money(amount, currency));
        cache.Publish(product);

        await context.SaveChangesAsync(cancellationToken);
        return product;
    }
}
