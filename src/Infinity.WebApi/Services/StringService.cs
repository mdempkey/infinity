namespace Infinity.WebApi.Services;

public class StringService : IStringService
{
    public string Reverse(string input) =>
        new string(input.Reverse().ToArray());
}