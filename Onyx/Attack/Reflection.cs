using System.Linq.Expressions;
using System.Reflection;

namespace Onyx.Attack;

public static partial class Reflection
{
    #region Getters and Setters
    public static FieldPackage GetField(object obj, string fieldName)
    {
        var type = obj.GetType();
        var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        return new(field, obj);
    }
    
    public static ReflectionResult SetField(object obj, string fieldName, object value)
    {
        var type = obj.GetType();
        var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null) return ReflectionResult.FieldNotFound;

        try { field.SetValue(obj, value); }
        catch (ArgumentException) { return ReflectionResult.IncorrectType;}
        return ReflectionResult.Success;
    }
    
    public static PropertyPackage GetProperty(object obj, string propertyName)
    {
        var type = obj.GetType();
        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        return new(property, obj);
    }
    
    public static ReflectionResult SetProperty(object obj, string propertyName, object value)
    {
        var type = obj.GetType();
        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (property == null) return ReflectionResult.PropertyNotFound;
        var setter = property.GetSetMethod(true);
        if (setter == null) return ReflectionResult.SetPropertyNotFound;
        
        setter.Invoke(obj, [value]);
        return ReflectionResult.Success;
    }

    public static MethodPackage GetMethod(object obj, string methodName)
    {
        var type = obj.GetType();
        var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (method == null)
            return new MethodPackage()
            {
                Access = AccessModifier.None,
                CompiledDelegate = null,
                Method = null,
                Name = methodName,
                Parameters = [],
                ReturnType = typeof(object),
                Result = ReflectionResult.MethodNotFound
            };
        return new MethodPackage(method);
    }
    #endregion
    #region Utility Methods (for inspection)

    public static AccessModifier GetAccessModifier(FieldInfo? field)
    {
        if (field == null) return AccessModifier.None;
        
        if (field.IsPublic) return AccessModifier.Public;
        if (field.IsPrivate) return AccessModifier.Private;
        if (field.IsFamily) return AccessModifier.Protected;
        if (field.IsAssembly) return AccessModifier.Internal;
        if (field.IsFamilyOrAssembly) return AccessModifier.ProtectedInternal;
        if (field.IsFamilyAndAssembly) return AccessModifier.PrivateProtected;
        return AccessModifier.None;
    }

    public static AccessModifier GetAccessModifier(PropertyInfo? property)
    {
        if (property == null) return AccessModifier.None;
        
        if (property.GetMethod != null && property.GetMethod.IsPublic) return AccessModifier.Public;
        if (property.GetMethod != null && property.GetMethod.IsPrivate) return AccessModifier.Private;
        if (property.GetMethod != null && property.GetMethod.IsFamily) return AccessModifier.Protected;
        if (property.GetMethod != null && property.GetMethod.IsAssembly) return AccessModifier.Internal;
        if (property.GetMethod != null && property.GetMethod.IsFamilyOrAssembly) return AccessModifier.ProtectedInternal;
        if (property.GetMethod != null && property.GetMethod.IsFamilyAndAssembly) return AccessModifier.PrivateProtected;
        return AccessModifier.None;
    }
    
    public static AccessModifier GetAccessModifier(MethodInfo? method)
    {
        if (method == null) return AccessModifier.None;
        
        if (method.IsPublic) return AccessModifier.Public;
        if (method.IsPrivate) return AccessModifier.Private;
        if (method.IsFamily) return AccessModifier.Protected;
        if (method.IsAssembly) return AccessModifier.Internal;
        if (method.IsFamilyOrAssembly) return AccessModifier.ProtectedInternal;
        if (method.IsFamilyAndAssembly) return AccessModifier.PrivateProtected;
        return AccessModifier.None;
    }
    #endregion
    #region Utility Methods

    public static VariablePackage FromObject(Expression<Func<object>> objPointer)
    {
        try
        {
            Expression body = objPointer.Body;

            if (body is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
            {
                body = unary.Operand;
            }

            if (body is not MemberExpression memberExpression)
                throw new ArgumentException("Expression does not point to a member.");

            string name = memberExpression.Member.Name;

            var converted = Expression.Convert(memberExpression, typeof(object));
            object? value = Expression.Lambda<Func<object>>(converted).Compile().Invoke();

            return new VariablePackage(name, value, AccessModifier.Irrelevant);
        }
        catch (Exception)
        {
            return new VariablePackage("unknown", null, AccessModifier.None, ReflectionResult.Failure);
        }
        
    }
    #endregion

    public static InspectionResult Inspect(object obj)
    {
        Type type = obj.GetType();
        List<FieldPackage> fields = new();
        List<PropertyPackage> properties = new();
        List<MethodPackage> methods = new();

        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            fields.Add(new(field, obj));
        
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            properties.Add(new(property, obj));

        
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            methods.Add(new()
            {
                Access = GetAccessModifier(method),
                Name = method.Name,
                Parameters = method.GetParameters().Select(x => new VariablePackage(x.Name ?? "unknown", null, AccessModifier.Irrelevant)).ToList(),
                ReturnType = method.ReturnType,
                Method = method,
                CompiledDelegate = method.IsSpecialName ? null : method.CreateDelegate(MethodPackage.BuildDelegateType(method))
            });
        }

        return new InspectionResult()
        {
            Fields = fields,
            Properties = properties,
            Type = type,
            Methods = methods
        };
    } 
}