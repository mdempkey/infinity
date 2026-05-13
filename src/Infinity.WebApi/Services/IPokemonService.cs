namespace Infinity.WebApi.Services;

public interface IPokemonService
{
    Task<IEnumerable<Models.Pokemon>> GetAllAsync();
    Task<Models.Pokemon?> GetByNameAsync(string name);
    Task<Stream?> GetCryAsync(string name);
}