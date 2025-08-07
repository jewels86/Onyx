using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Onyx.Attack.Reflection;

namespace Onyx.Attack;

public partial class Registry
{
    public static (List<WeakVariablePackage>, List<Type>) Enumerate(Type type)
    {
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).ToList();
        fields.AddRange(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic).Where(f => f.GetRawConstantValue() is not null));
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).ToList();
        properties.AddRange(type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic).Where(p => p.GetMethod != null && p.GetMethod.IsStatic));
        
        var results = new List<WeakVariablePackage>();
        foreach (var field in fields) results.Add(new(field.Name, field.GetRawConstantValue(), GetAccessModifier(field)));
        foreach (var prop in properties)
        {
            object? value = null;
            try { value = prop.GetValue(null); }
            catch { value = null; }
            results.Add(new(prop.Name, value, GetAccessModifier(prop)));
        }
        
        results = results.DistinctBy(r => r.Value).Where(r => r.Value != null).ToList();
        return (results, results.Select(x => x.Type).Distinct().ToList());
    }
    
    public static (List<WeakVariablePackage>, List<Type>) Enumerate(object obj)
    {
        var type = obj.GetType();
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).ToList();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(p => p.GetMethod != null).ToList();
        
        var results = new List<WeakVariablePackage>();
        foreach (var field in fields) results.Add(new(field.Name, field.GetValue(obj), GetAccessModifier(field)));
        foreach (var prop in properties)
        {
            object? value = null;
            try { value = prop.GetValue(obj); }
            catch { value = null; }
            results.Add(new(prop.Name, value, GetAccessModifier(prop)));
        }
        
        results = results.DistinctBy(r => r.Value).Where(r => r.Value != null).ToList();
        return (results, results.Select(x => x.Type).Distinct().ToList());
    }
}