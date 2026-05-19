using Reference.VerticalSlice.Domain;

namespace Reference.VerticalSlice.Shared.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await context.Database.EnsureCreatedAsync();

        if (await context.Authors.AnyAsync())
        {
            return;
        }

        var ursula = Author.Create("Ursula K. Le Guin");
        var octavia = Author.Create("Octavia E. Butler");

        context.Authors.AddRange(ursula, octavia);
        context.Books.AddRange(
            Book.Create(ursula.Id, "A Wizard of Earthsea"),
            Book.Create(octavia.Id, "Parable of the Sower"));

        await context.SaveChangesAsync();
    }
}
