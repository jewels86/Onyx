namespace Onyx.Shared;

public static class GeneralUtilities
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
    
    public static string GetCSharpTypeName(Type t)
    {
        if (t.IsGenericType)
        {
            var genericType = t.GetGenericTypeDefinition();
            var typeName = genericType.Name.Split('`')[0];
            var genericArgs = t.GetGenericArguments().Select(GetCSharpTypeName);
            return $"{typeName}<{string.Join(", ", genericArgs)}>";
        }
        if (t == typeof(int)) return "int";
        if (t == typeof(string)) return "string";
        if (t == typeof(bool)) return "bool";
        if (t == typeof(object)) return "object";
        if (t.IsArray) return GetCSharpTypeName(t.GetElementType()!) + "[]";
        return t.FullName ?? t.Name;
    }
}