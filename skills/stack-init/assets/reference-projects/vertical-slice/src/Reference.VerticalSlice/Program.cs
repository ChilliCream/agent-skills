using Reference.VerticalSlice.Features.Authors.CreateAuthor;
using Reference.VerticalSlice.Features.Authors.GetAuthorById;
using Reference.VerticalSlice.Features.Authors.ListAuthors;
using Reference.VerticalSlice.Features.Authors.Types;
using Reference.VerticalSlice.Features.Authors.UpdateAuthor;
using Reference.VerticalSlice.Features.Books.CreateBook;
using Reference.VerticalSlice.Features.Books.GetBookById;
using Reference.VerticalSlice.Features.Books.ListBooksByAuthor;
using Reference.VerticalSlice.Features.Books.Types;
using Reference.VerticalSlice.Features.Books.UpdateBook;
using Reference.VerticalSlice.Shared.Persistence;
using Reference.VerticalSlice.Shared.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("VerticalSlice")
        ?? "Data Source=vertical-slice.db"));

builder.Services.AddAuthorization(LibraryPolicies.Configure);

builder.Services
    .AddMediator()
    .AddVerticalSlice();

builder.Services
    .AddGraphQLServer()
    .AddAuthorization()
    .AddMutationConventions()
    .AddTypes(
    [
        typeof(AuthorType),
        typeof(BookType),
        typeof(GetAuthorByIdQueryType),
        typeof(ListAuthorsQueryType),
        typeof(CreateAuthorMutation),
        typeof(UpdateAuthorMutation),
        typeof(GetBookByIdQueryType),
        typeof(ListBooksByAuthorQueryType),
        typeof(CreateBookMutation),
        typeof(UpdateBookMutation)
    ]);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await DatabaseSeeder.SeedAsync(app.Services);
}

app.UseAuthorization();
app.MapGraphQL();
app.MapGet("/", () => Results.Redirect("/graphql"));

app.Run();

public partial class Program;
