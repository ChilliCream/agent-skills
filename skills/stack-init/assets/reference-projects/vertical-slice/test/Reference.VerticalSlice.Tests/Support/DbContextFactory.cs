using Microsoft.EntityFrameworkCore;
using Reference.VerticalSlice.Shared.Persistence;

namespace Reference.VerticalSlice.Tests.Support;

public static class DbContextFactory
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
