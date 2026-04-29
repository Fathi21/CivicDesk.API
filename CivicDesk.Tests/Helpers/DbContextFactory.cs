using Microsoft.EntityFrameworkCore;
using CivicDesk.API.Data;

namespace CivicDesk.Tests.Helpers;

public static class DbContextFactory
{
    /// <summary>
    /// Creates an isolated in-memory database. Each call gets its own DB
    /// so tests cannot affect each other.
    /// </summary>
    public static AppDbContext Create() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
