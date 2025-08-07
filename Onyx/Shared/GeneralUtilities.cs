namespace Onyx.Shared;

public static class GeneralUtilities
{
    public static string NewGUID(int length, bool sanitize = false)
    {
        length = Math.Max(length, 16);
        string result = Guid.NewGuid().ToString().Substring(0, length);
        if (sanitize) result = result.Replace("-", "");
        return result;
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

    public static string GetCSharpTypeName(object? obj)
    {
        Type? type = obj?.GetType();
        if (type is null) return "null";
        if (obj is DynamicTypeName dtn) return dtn.Name;
        return GetCSharpTypeName(type);

    }
    
    public static T TryCatch<T>(Func<T> func, Action<Exception, T>? errorHandler, T defaultValue = default!)
    {
        try
        {
            return func();
        }
        catch (Exception ex)
        {
            errorHandler?.Invoke(ex, defaultValue);
            return defaultValue;
        }
    }

    public class DynamicTypeName(string name)
    {
        public string Name { get; set; } = name;
    }
}