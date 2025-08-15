using System.Reflection;
using Microsoft.CodeAnalysis;
using Mono.Cecil;
using HarmonyLib;
using Onyx.Shared;

namespace Onyx.Shared;

public static class PostCompilation
{
    public static TypeDefinition ImportType(TypeDefinition type, ModuleDefinition target)
    {
        var newType = new TypeDefinition(
            type.Namespace, 
            type.Name, 
            type.Attributes, 
            target.ImportReference(type.BaseType));
        foreach (var field in type.Fields)
        {
            newType.Fields.Add(new FieldDefinition(
                field.Name, 
                field.Attributes, 
                target.ImportReference(field.FieldType)));
        }
        foreach (var method in type.Methods)
        {
            var newMethod = new MethodDefinition(
                method.Name, 
                method.Attributes, 
                target.ImportReference(method.ReturnType));
            foreach (var param in method.Parameters)
            {
                newMethod.Parameters.Add(new ParameterDefinition(
                    param.Name, 
                    param.Attributes, 
                    target.ImportReference(param.ParameterType)));
            }
            newType.Methods.Add(newMethod);
        }
        foreach (var property in type.Properties)
        {
            var newProperty = new PropertyDefinition(
                property.Name, 
                property.Attributes, 
                target.ImportReference(property.PropertyType));
            if (property.GetMethod != null)
            {
                newProperty.GetMethod = target.ImportReference(property.GetMethod).Resolve();
            }
            if (property.SetMethod != null)
            {
                newProperty.SetMethod = target.ImportReference(property.SetMethod).Resolve();
            }
            newType.Properties.Add(newProperty);
        }

        return newType;
    }

    public static string? ResolveAssemblyPath(AssemblyNameReference reference)
    {
        var tpa = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (tpa is null) return null;
        return tpa.Split(Path.PathSeparator)
            .FirstOrDefault(path =>
                Path.GetFileNameWithoutExtension(path).Equals(reference.Name, StringComparison.OrdinalIgnoreCase));
    }
    
    public static AssemblyDefinition? ResolveAssembly(AssemblyNameReference reference, ReaderParameters? parameters = null)
    {
        var path = ResolveAssemblyPath(reference);
        if (path is null) return null;

        parameters ??= new ReaderParameters { ReadSymbols = false };
        return AssemblyDefinition.ReadAssembly(path, parameters);
    }

    public static Dictionary<string, string> Obfuscate(List<IMemberDefinition> defs, Func<string, string>? obfuscateName = null)
    {
        if (obfuscateName == null)
            obfuscateName = name => GeneralUtilities.NewGUID(16, true);
        Dictionary<string, string> obfuscatedNames = [];
        
        foreach (var def in defs)
        {
            if (def.DeclaringType.IsNested || (def is TypeDefinition typeDef && typeDef.IsNested)) 
                continue; // Skip nested types

            string newName;
            if (!obfuscatedNames.TryGetValue(def.Name, out newName!))
            {
                newName = obfuscateName(def.Name);
                obfuscatedNames[$"{MemberDefinitionPrefix(def)}{def.Name}"] = newName;
            }
            def.Name = newName;
        }
        
        return obfuscatedNames;
    }

    public static List<TypeDefinition> GetTypesToObfuscate(List<TypeDefinition> types)
    {
        return types
            .Where(t => t.CustomAttributes.Any(a => a.AttributeType.Name == "ObfuscateAttribute"))
            .ToList();
    }
    public static List<IMemberDefinition> GetMembersToObfuscate(List<IMemberDefinition> members)
    {
        return members
            .Where(m => m.CustomAttributes.Any(a => a.AttributeType.Name == "ObfuscateAttribute"))
            .ToList();
    }

    public static Dictionary<string, string> ObfuscateWithAttribute(AssemblyDefinition asm,
        Func<string, string>? obfuscateName = null)
    {
        if (obfuscateName == null)
            obfuscateName = name => GeneralUtilities.NewGUID(16, true);
        
        var typesToObfuscate = GetTypesToObfuscate(asm.MainModule.Types.ToList());
        var membersToObfuscate = GetMembersToObfuscate(
            asm.MainModule.Types.SelectMany(t => t.Methods.Cast<IMemberDefinition>())
                .Concat(asm.MainModule.Types.SelectMany(t => t.Fields.Cast<IMemberDefinition>()))
                .Concat(asm.MainModule.Types.SelectMany(t => t.Properties.Cast<IMemberDefinition>()))
                .Concat(asm.MainModule.Types.SelectMany(t => t.Events.Cast<IMemberDefinition>())).ToList());

        var obfuscatedNames = Obfuscate(typesToObfuscate.Cast<IMemberDefinition>().Concat(membersToObfuscate).ToList(), obfuscateName);
        
        foreach (var type in typesToObfuscate)
        {
            asm.MainModule.Types.Remove(type);
            asm.MainModule.Types.Add(ImportType(type, asm.MainModule));
        }

        return obfuscatedNames;
    }

    public static string MemberDefinitionPrefix(IMemberDefinition def)
    {
        if (def is TypeDefinition) return "type:";
        if (def is MethodDefinition) return "method:";
        if (def is FieldDefinition) return "field:";
        if (def is PropertyDefinition) return "property:";
        if (def is EventDefinition) return "event:";
        return "unknown:";
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct |
                    AttributeTargets.Method | AttributeTargets.Property |
                    AttributeTargets.Field | AttributeTargets.Event)]
    public class ObfuscateAttribute : Attribute
    {
    }
}