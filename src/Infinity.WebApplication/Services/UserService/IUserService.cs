using Infinity.WebApplication.Models;

namespace Infinity.WebApplication.Services.UserService;

public interface IUserService
{
    Task<User?> RegisterAsync(string username, string email, string password);
    Task<User?> LoginAsync(string username, string password);
    Task<User?> GetByIdAsync(Guid id);
}
