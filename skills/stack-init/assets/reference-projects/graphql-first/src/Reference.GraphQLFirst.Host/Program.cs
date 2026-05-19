using Microsoft.EntityFrameworkCore;
using Reference.GraphQLFirst.Application;
using Reference.GraphQLFirst.GraphQL;
using Reference.GraphQLFirst.Infrastructure;
using Reference.GraphQLFirst.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("ReferenceGraphQLFirst")
    ?? "Data Source=reference-graphql-first.db";

builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Authors.Read", policy => policy.RequireAuthenticatedUser());
    options.AddPolicy("Authors.Create", policy => policy.RequireAuthenticatedUser());
    options.AddPolicy("Books.Read", policy => policy.RequireAuthenticatedUser());
    options.AddPolicy("Books.Create", policy => policy.RequireAuthenticatedUser());
});

builder.Services.AddReferencePersistence(connectionString);

builder.Services.AddReferenceGraphQLFirstApplication();
builder.Services.AddReferenceGraphQLFirstGraphQL();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<ReferenceDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapGraphQL();

await app.RunAsync();
