using Reference.CleanArchitecture.Application;
using Reference.CleanArchitecture.Application.Common;
using Reference.CleanArchitecture.GraphQL;
using Reference.CleanArchitecture.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        BookStorePolicies.AuthorsRead,
        policy => policy.RequireAuthenticatedUser());
    options.AddPolicy(
        BookStorePolicies.AuthorsWrite,
        policy => policy.RequireAuthenticatedUser());
    options.AddPolicy(
        BookStorePolicies.BooksRead,
        policy => policy.RequireAuthenticatedUser());
    options.AddPolicy(
        BookStorePolicies.BooksWrite,
        policy => policy.RequireAuthenticatedUser());
});

builder.Services.AddReferenceCleanArchitectureApplication();
builder.Services.AddReferenceCleanArchitectureInfrastructure(builder.Configuration);
builder.Services.AddReferenceCleanArchitectureGraphQL();

var app = builder.Build();

app.MapGraphQL();
app.MapGet("/", () => Results.Redirect("/graphql"));

app.Run();

public partial class Program;
