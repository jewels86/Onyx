namespace Onyx.Shared;

public class GeneralUtilities
{
    public static string NewGUID(int length)
    {
        length = Math.Max(length, 16);
        return Guid.NewGuid().ToString().Substring(0, length);
    }
    
    public static string FromStrings(IEnumerable<string> strings, string separator = "")
    {
        return string.Join(separator, strings);
    }

    public static string RemoveIllegalCharacters(string input)
    {
        return new string(input
            .Where(c => char.IsLetterOrDigit(c) || c == '_' || c == ' ')
            .ToArray())
            .Trim();
    }
}