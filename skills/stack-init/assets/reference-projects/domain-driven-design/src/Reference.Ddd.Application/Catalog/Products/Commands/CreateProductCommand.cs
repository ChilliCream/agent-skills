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

public sealed record CreateProductCommand(
    ClaimsPrincipal User,
    string Sku,
    string Name,
    string? Description,
    decimal PriceAmount,
    string Currency)
    : ICommand<Product>;

public sealed class CreateProductCommandHandler(
    ICatalogDbContext context,
    IPromiseCache cache,
    IAuthorizationService authorization)
    : ICommandHandler<CreateProductCommand, Product>
{
    public async ValueTask<Product> HandleAsync(
        CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        var (user, skuValue, name, description, priceAmount, currency) = command;

        if (user.Identity is not { IsAuthenticated: true })
        {
            throw new UnauthorizedAccessException();
        }

        var result = await authorization.AuthorizeAsync(user, null, ReferencePolicies.CatalogManage);
        if (!result.Succeeded)
        {
            throw new UnauthorizedAccessException();
        }

        var sku = new Sku(skuValue);
        if (await context.Products.AnyAsync(x => x.Sku.Value == sku.Value, cancellationToken))
        {
            throw new DuplicateSkuException(sku.Value);
        }

        var product = Product.Create(sku, name, description, new Money(priceAmount, currency));
        context.Products.Add(product);
        cache.Publish(product);

        await context.SaveChangesAsync(cancellationToken);
        return product;
    }
}
