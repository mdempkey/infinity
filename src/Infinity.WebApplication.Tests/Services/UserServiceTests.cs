using Infinity.WebApplication.Data;
using Infinity.WebApplication.Services.UserService;
using Microsoft.EntityFrameworkCore;

namespace Infinity.WebApplication.Tests.Services;

public class UserServiceTests
{
    private UserDbContext _db = null!;
    private IUserService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _db = new UserDbContext(
            new DbContextOptionsBuilder<UserDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        // workFactor: 4 keeps BCrypt fast in tests
        _sut = new UserService(_db, workFactor: 4);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task RegisterAsync_WithValidCredentials_ReturnsUser()
    {
        var user = await _sut.RegisterAsync("testuser", "test@example.com", "password123");

        Assert.That(user, Is.Not.Null);
        Assert.That(user!.Username, Is.EqualTo("testuser"));
        Assert.That(user.Email, Is.EqualTo("test@example.com"));
        Assert.That(user.PasswordHash, Is.Not.EqualTo("password123"));
    }

    [Test]
    public async Task RegisterAsync_WithDuplicateUsername_ReturnsNull()
    {
        await _sut.RegisterAsync("testuser", "first@example.com", "password123");

        var duplicate = await _sut.RegisterAsync("testuser", "second@example.com", "password456");

        Assert.That(duplicate, Is.Null);
    }

    [Test]
    public async Task RegisterAsync_WithDuplicateEmail_ReturnsNull()
    {
        await _sut.RegisterAsync("firstuser", "same@example.com", "password123");

        var duplicate = await _sut.RegisterAsync("seconduser", "same@example.com", "password456");

        Assert.That(duplicate, Is.Null);
    }

    [Test]
    public async Task LoginAsync_WithCorrectPassword_ReturnsUser()
    {
        await _sut.RegisterAsync("testuser", "test@example.com", "correctpassword");

        var user = await _sut.LoginAsync("testuser", "correctpassword");

        Assert.That(user, Is.Not.Null);
        Assert.That(user!.Username, Is.EqualTo("testuser"));
    }

    [Test]
    public async Task LoginAsync_WithWrongPassword_ReturnsNull()
    {
        await _sut.RegisterAsync("testuser", "test@example.com", "correctpassword");

        var user = await _sut.LoginAsync("testuser", "wrongpassword");

        Assert.That(user, Is.Null);
    }

    [Test]
    public async Task LoginAsync_WithUnknownUsername_ReturnsNull()
    {
        var user = await _sut.LoginAsync("nobody", "password");

        Assert.That(user, Is.Null);
    }

    [Test]
    public async Task GetByIdAsync_WithValidId_ReturnsUser()
    {
        var created = await _sut.RegisterAsync("testuser", "test@example.com", "password123");

        var found = await _sut.GetByIdAsync(created!.Id);

        Assert.That(found, Is.Not.Null);
        Assert.That(found!.Id, Is.EqualTo(created.Id));
    }

    [Test]
    public async Task GetByIdAsync_WithUnknownId_ReturnsNull()
    {
        var found = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.That(found, Is.Null);
    }
}
