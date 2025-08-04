using System.Linq.Expressions;
using System.Reflection;

namespace Onyx.Attack;

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

        public FieldPackage(FieldInfo? field, object obj)
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

        public PropertyPackage(PropertyInfo? property, object obj)
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
    
    public struct InspectionResult
    {
        public Type Type { get; set; }
        public List<FieldPackage> Fields { get; set; }
        public List<PropertyPackage> Properties { get; set; }
        public List<MethodPackage> Methods { get; set; }
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