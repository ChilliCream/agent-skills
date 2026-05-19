using Reference.Ddd.Application;
using Reference.Ddd.Application.Security;
using Reference.Ddd.GraphQL;
using Reference.Ddd.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(ReferencePolicies.CatalogRead, policy => policy.RequireAuthenticatedUser());
    options.AddPolicy(ReferencePolicies.CatalogManage, policy => policy.RequireAuthenticatedUser());
    options.AddPolicy(ReferencePolicies.OrdersRead, policy => policy.RequireAuthenticatedUser());
    options.AddPolicy(ReferencePolicies.OrdersWrite, policy => policy.RequireAuthenticatedUser());
});

builder.Services.AddReferenceDddInfrastructure(builder.Configuration);
builder.Services.AddReferenceDddApplication();
builder.Services.AddReferenceDddGraphQL();

var app = builder.Build();

app.MapGraphQL();

app.Run();
