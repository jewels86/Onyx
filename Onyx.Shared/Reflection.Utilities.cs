using System.Linq.Expressions;
using System.Reflection;

namespace Onyx.Shared;

public static partial class Reflection
{
    public enum ReflectionResult
    {
        FieldNotFound,
        PropertyNotFound,
        SetPropertyNotFound,
        Success,
        Irrelevant,
        PropertyUnreadable,
        MethodNotFound,
        IncorrectType,
        Failure
    } 

    public interface IVariablePackage
    {
        public string Name { get; set; }
        public Type Type { get; set; }
        public object? Value { get; set; }
        public AccessModifier Access { get; set; }
        public ReflectionResult Result { get; set; }
    }

    public class FieldPackage : IVariablePackage
    {
        public FieldInfo? Field { get; init; }
        
        public string Name { get; set; }
        public Type Type { get; set; }
        public object? Value { get; set; }
        public AccessModifier Access { get; set; }
        public ReflectionResult Result { get; set; }

        public FieldPackage(FieldInfo? field, object? obj)
        {
            Name = field?.Name ?? "unknown";
            Type = field?.FieldType ?? typeof(object);
            Value = field?.GetValue(obj) ?? ReflectionResult.FieldNotFound;
            Access = GetAccessModifier(field);
            Field = field;
            Result = field == null ? ReflectionResult.FieldNotFound : ReflectionResult.Success;
        }

        public FieldPackage(FieldInfo? field)
        {
            Name = field?.Name ?? "unknown";
            Type = field?.FieldType ?? typeof(object);
            Value = ReflectionResult.Irrelevant;
            Access = GetAccessModifier(field);
            Field = field;
            Result = field == null ? ReflectionResult.FieldNotFound : ReflectionResult.Success;
        }
        
        public FieldPackage(string name, Type type, object? value, AccessModifier access, ReflectionResult result = ReflectionResult.Success)
        {
            Name = name;
            Type = type;
            Value = value;
            Access = access;
            Result = result;
            Field = null;
        }
    }
    
    public class PropertyPackage : IVariablePackage
    {
        public PropertyInfo? Property { get; init; }
        
        public string Name { get; set; }
        public Type Type { get; set; }
        public object? Value { get; set; }
        public AccessModifier Access { get; set; }
        public ReflectionResult Result { get; set; }

        public PropertyPackage(PropertyInfo? property, object? obj)
        {
            Name = property?.Name ?? "unknown";
            Type = property?.PropertyType ?? typeof(object);
            bool readable;
            try
            {
                Value = property?.GetValue(obj);
                readable = true;
            } 
            catch (Exception)
            {
                Value = null;
                readable = false;
            }
            Access = GetAccessModifier(property);
            Property = property;
            Result = property == null ? ReflectionResult.PropertyNotFound : 
                readable ? ReflectionResult.Success : ReflectionResult.PropertyUnreadable;
        }
        
        public PropertyPackage(PropertyInfo? property)
        {
            Name = property?.Name ?? "unknown";
            Type = property?.PropertyType ?? typeof(object);
            Value = ReflectionResult.Irrelevant;
            Access = GetAccessModifier(property);
            Property = property;
            Result = property == null ? ReflectionResult.PropertyNotFound : ReflectionResult.Success;
        }
        
        public PropertyPackage(string name, Type type, object? value, AccessModifier access, ReflectionResult result = ReflectionResult.Success)
        {
            Name = name;
            Type = type;
            Value = value;
            Access = access;
            Result = result;
            Property = null;
        }
    }
    
    public class VariablePackage : IVariablePackage
    {
        public string Name { get; set; }
        public Type Type { get; set; }
        public object? Value { get; set; }
        public AccessModifier Access { get; set; }
        public ReflectionResult Result { get; set; }

        public VariablePackage(string name, object? value, AccessModifier access, ReflectionResult result = ReflectionResult.Success)
        {
            Name = name;
            Type = value?.GetType() ?? typeof(object);
            Value = value;
            Access = access;
            Result = result;
        }
    }

    public struct MethodPackage
    {
        public string Name { get; set; }
        public AccessModifier Access { get; set; }
        public Type ReturnType { get; set; }
        public List<VariablePackage> Parameters { get; set; }
        public MethodInfo? Method { get; set; }
        public Delegate? CompiledDelegate { get; set; }
        public Type? DelegateType => CompiledDelegate?.GetType();
        public ReflectionResult Result { get; set; } = ReflectionResult.Success;

        public MethodPackage(MethodInfo info)
        {
            Access = GetAccessModifier(info);
            CompiledDelegate = info.CreateDelegate(BuildDelegateType(info));
            Method = info;
            Name = info.Name;
            ReturnType = info.ReturnType;
            Parameters = info.GetParameters()
                .Select(x => new VariablePackage(x.Name ?? "unknown", null, AccessModifier.Irrelevant)).ToList();
        }
        
        public MethodPackage(string name, AccessModifier access, Type returnType, List<VariablePackage> parameters, MethodInfo? method = null)
        {
            Name = name;
            Access = access;
            ReturnType = returnType;
            Parameters = parameters;
            Method = method;
            CompiledDelegate = method?.CreateDelegate(BuildDelegateType(method));
            Result = method == null ? ReflectionResult.MethodNotFound : ReflectionResult.Success;
        }
        
        public static Type BuildDelegateType(MethodInfo method)
        {
            var parameterTypes = method.GetParameters().Select(p => p.ParameterType).ToList();
            if (!method.IsStatic)
                parameterTypes.Insert(0, method.DeclaringType!);
            return Expression.GetDelegateType(parameterTypes.Concat([method.ReturnType]).ToArray());
        }
    }
    
    public class WeakVariablePackage : VariablePackage
    {
        public WeakReference<object?> Reference { get; set; }

        public new object? Value
        {
            get
            {
                if (Reference.TryGetTarget(out var target))
                    return target;
                return null;
            }
            set { Reference = new(value); }
        }
        
        public WeakVariablePackage(string name, object? value, AccessModifier access, ReflectionResult result = ReflectionResult.Success)
            : base(name, value, access, result)
        {
            Reference = new(value);
        }
    }
    
    public struct InspectionResult
    {
        public Type Type { get; init; }
        public List<FieldPackage> Fields { get; init; }
        public List<PropertyPackage> Properties { get; init; }
        public List<MethodPackage> Methods { get; init; }

        public string ToFormattedString(Func<IVariablePackage, string>? extra = null)
        {
            if (extra == null)
                extra = _ => string.Empty;
            return $"Object is of type: {Type.FullName}\n" +
                   $"Fields: {Fields.Count}\n" +
                   string.Join("\n", Fields.Select(f => $"  - {f.Name} ({f.Type.Name}) [{f.Access}] {extra(f)}")) + "\n" +
                   $"Properties: {Properties.Count}\n" +
                   string.Join("\n", Properties.Select(p => $"  - {p.Name} ({p.Type.Name}) [{p.Access}] {extra(p)}")) + "\n" +
                   $"Methods: {Methods.Count}\n" +
                   string.Join("\n", Methods.Select(m => $"  - {m.Name} ({m.ReturnType.Name}) [{m.Access}]"));
        }

        public override string ToString()
        {
            return ToFormattedString();
        }
    }
    
    public static MethodAttributes AccessModifierToMethodAttributes(this AccessModifier access)
    {
        MethodAttributes attributes = MethodAttributes.PrivateScope;
        if (access.HasFlag(AccessModifier.Public)) attributes |= MethodAttributes.Public;
        if (access.HasFlag(AccessModifier.Private)) attributes |= MethodAttributes.Private;
        if (access.HasFlag(AccessModifier.Protected)) attributes |= MethodAttributes.Family;
        if (access.HasFlag(AccessModifier.Internal)) attributes |= MethodAttributes.Assembly;
        if (access.HasFlag(AccessModifier.ProtectedInternal)) attributes |= MethodAttributes.FamORAssem;
        if (access.HasFlag(AccessModifier.PrivateProtected)) attributes |= MethodAttributes.FamANDAssem;
        if (access.HasFlag(AccessModifier.Static)) attributes |= MethodAttributes.Static;
        if (access.HasFlag(AccessModifier.Abstract)) attributes |= MethodAttributes.Abstract;
        if (access.HasFlag(AccessModifier.Virtual)) attributes |= MethodAttributes.Virtual;
        if (access.HasFlag(AccessModifier.Override)) attributes |= MethodAttributes.NewSlot | MethodAttributes.Virtual;
        if (access.HasFlag(AccessModifier.Sealed)) attributes |= MethodAttributes.Final;

        return attributes;
    }

    #region Try Get Values
    public static object? TryGetRawConstantValue(this FieldInfo field)
    {
        try
        {
            return field.GetRawConstantValue();
        }
        catch
        {
            return null;
        }
    }
    public static object? TryGetRawConstantValue(this PropertyInfo property)
    {
        try
        {
            var getMethod = property.GetGetMethod(true);
            if (getMethod != null && getMethod.IsStatic && getMethod.GetParameters().Length == 0)
            {
                return getMethod.Invoke(null, null);
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public static object? TryGetValue(this FieldInfo field, object? obj)
    {
        try
        {
            return field.GetValue(obj);
        }
        catch
        {
            return null;
        }
    }

    public static object? TryGetValue(this PropertyInfo property, object? obj)
    {
        try
        {
            return property.GetValue(obj);
        }
        catch
        {
            return null;
        }
    }
    #endregion
}

[Flags]
public enum AccessModifier
{
    Public = 2,
    Private = 4,
    Protected = 8,
    Internal = 16,
    ProtectedInternal = 32,
    PrivateProtected = 64,
    None = 128, Irrelevant = 256,
    Static = 512, Abstract = 1024, Virtual = 2048, Override = 4096, Sealed = 8192
}