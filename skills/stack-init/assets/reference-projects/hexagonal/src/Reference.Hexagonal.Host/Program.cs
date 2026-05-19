using HotChocolate.Types.Relay;
using Microsoft.EntityFrameworkCore;
using Mocha.Mediator;
using Reference.Hexagonal.Adapters.Persistence;
using Reference.Hexagonal.Core.Ports.In;
using Reference.Hexagonal.Core.UseCases;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization();

builder.Services.AddScoped<ICreateAuthor, CreateAuthorUseCase>();
builder.Services.AddScoped<IGetAuthorById, GetAuthorByIdUseCase>();
builder.Services.AddScoped<IRegisterBook, RegisterBookUseCase>();
builder.Services.AddScoped<IRenameBook, RenameBookUseCase>();
builder.Services.AddScoped<IGetBookById, GetBookByIdUseCase>();
builder.Services.AddScoped<IGetBooksByAuthor, GetBooksByAuthorUseCase>();

builder.Services.AddHexagonalPersistence(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("Library")
        ?? "Data Source=reference-hexagonal.db"));

builder.Services
    .AddMediator()
    .AddHexagonalGraphQL();

builder.Services
    .AddGraphQLServer()
    .AddHexagonalGraphQLTypes()
    .AddMutationConventions()
    .ConfigureSchema(schema => schema.AddGlobalObjectIdentification())
    .AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapGet("/", () => Results.Redirect("/graphql"));
app.MapGraphQL();

await app.RunAsync();
