using Infinity.WebApplication.Data;
using Infinity.WebApplication.Models;
using Microsoft.EntityFrameworkCore;

namespace Infinity.WebApplication.Tests.Services;

public class UserDbContextTests
{
    private static UserDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<UserDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Test]
    public async Task CanSaveAndRetrieveUser()
    {
        await using var db = CreateContext();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "luke",
            Email = "luke@rebellion.org",
            PasswordHash = "hashed",
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var found = await db.Users.FindAsync(user.Id);
        Assert.That(found, Is.Not.Null);
        Assert.That(found!.Username, Is.EqualTo("luke"));
    }
}
