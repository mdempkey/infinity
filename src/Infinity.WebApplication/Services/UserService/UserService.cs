using Infinity.WebApplication.Data;
using Infinity.WebApplication.Models;
using Microsoft.EntityFrameworkCore;

namespace Infinity.WebApplication.Services.UserService;

public sealed class UserService(UserDbContext db, int workFactor = 11) : IUserService
{
    public async Task<User?> RegisterAsync(string username, string email, string password)
    {
        bool taken = await db.Users.AnyAsync(u => u.Username == username || u.Email == email);
        if (taken) return null;

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor),
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public async Task<User?> LoginAsync(string username, string password)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user is null) return null;
        return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash) ? user : null;
    }

    public async Task<User?> GetByIdAsync(Guid id) =>
        await db.Users.FindAsync(id);
}
