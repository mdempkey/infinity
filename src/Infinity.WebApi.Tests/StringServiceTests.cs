using Infinity.WebApi.Services;

namespace Infinity.WebApi.Tests;

public class StringServiceTests
{
    private readonly StringService _stringService = new();

    [Theory]
    [InlineData("hello", "olleh")]
    [InlineData("", "")]
    [InlineData(null, "")]
    [InlineData("a", "a")]
    [InlineData("racecar", "racecar")]
    [InlineData("hello world", "dlrow olleh")]
    [InlineData("hello  world", "dlrow  olleh")]
    [InlineData(" hello world", "dlrow olleh ")]
    [InlineData("hello world ", " dlrow olleh")]
    public void Reverse_WithVariousInputs_ReturnsExpectedResult(string? input, string expected)
    {
        var result = _stringService.Reverse(input!);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("hello", "hello")]
    [InlineData("", "")]
    [InlineData(" ", "")]
    [InlineData(null, "")]
    [InlineData("a", "a")]
    [InlineData("hello world", "world hello")]
    [InlineData("Hello World", "World Hello")]
    [InlineData("The quick brown fox", "fox brown quick The")]
    [InlineData(" hello world ", "world hello")]
    [InlineData("a    b   c", "c b a")]
    [InlineData("Tom D'pint", "D'pint Tom")]
    [InlineData("tabbed\thello", "hello tabbed")]
    [InlineData("One line\nTwo line", "line One\nline Two")]
    public void ReverseWords_WithVariousInputs(string? input, string expected)
    {
        var result = _stringService.ReverseWords(input!);
        Assert.Equal(expected, result);
    }
}
