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
        public string Name { get; init; }
        public AccessModifier Access { get; init; }
        public Type ReturnType { get; init; }
        public List<VariablePackage> Parameters { get; init; }
        public MethodInfo? Method { get; init; }
        public Delegate? CompiledDelegate { get; init; }
        public Type? DelegateType => CompiledDelegate?.GetType();
        
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
}

public enum AccessModifier
{
    Public,
    Private,
    Protected,
    Internal,
    ProtectedInternal,
    PrivateProtected,
    None, Irrelevant
}