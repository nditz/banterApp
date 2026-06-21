using BanterApp.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace BanterApp.Api.Tests.Infrastructure;

internal static class TestDbContextFactory
{
    public static AppDbContext Create(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
