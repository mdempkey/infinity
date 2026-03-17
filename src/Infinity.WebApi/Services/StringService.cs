namespace Infinity.WebApi.Services;

public class StringService : IStringService
{
    public string Reverse(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        char[] charArray = input.ToCharArray();
        Array.Reverse(charArray);
        return new string(charArray);
    }

    public string ReverseWords(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }
        string[] outputLines = [];
        string[] lines = input.Trim().Split("\n", StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in lines)
        {
            string[] words = line.Trim().Split([" ", "\t"], StringSplitOptions.RemoveEmptyEntries);
            words = words.Reverse().ToArray();
            outputLines = outputLines.Append(string.Join(" ", words)).ToArray();
        }
        return string.Join("\n", outputLines);
    }
}
