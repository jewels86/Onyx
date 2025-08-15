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
    
    public static void AsmInject(AssemblyDefinition target, MemoryStream compiled)
    {
        var source = AssemblyDefinition.ReadAssembly(compiled, new ReaderParameters { ReadSymbols = false});
        foreach (var module in source.Modules)
        {
            foreach (var type in module.Types.Where(t => !t.IsRuntimeSpecialName))
            {
                if (type.Name == "<Module>") continue;
                var importedType = ImportType(type, target.MainModule);
                target.MainModule.Types.Add(importedType);
            }
        }
    }

    public static void MethodInject(MethodInfo target, MethodInfo? prefix = null, MethodInfo? postfix = null, Harmony? harmony = null)
    {
        harmony ??= new Harmony($"onyx-attack-{GeneralUtilities.NewGUID(8, true)}");
        harmony.Patch(target, prefix: new HarmonyMethod(prefix), postfix: new HarmonyMethod(postfix));
    }

    public static void MethodInject(string code, MethodInfo target, Harmony? harmony = null)
    {
        
    }

    public static string? ResolveAssemblyPath(AssemblyNameReference reference)
    {
        var tpa = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (tpa is null) return null;
        return tpa.Split(Path.PathSeparator)
            .FirstOrDefault(path =>
                Path.GetFileNameWithoutExtension(path).Equals(reference.Name, StringComparison.OrdinalIgnoreCase));
    }

    public static List<MetadataReference> ExtractReferences(AssemblyDefinition target)
    {
        var refs = new List<MetadataReference>();
        if (!string.IsNullOrEmpty(target.MainModule.FileName)) 
            refs.Add(MetadataReference.CreateFromFile(target.MainModule.FileName));

        foreach (var reference in target.MainModule.AssemblyReferences)
        {
            string? resolved = ResolveAssemblyPath(reference);
            if (resolved != null && File.Exists(resolved))
            {
                refs.Add(MetadataReference.CreateFromFile(resolved));
            }
        }

        return refs;
    }

    public static List<MetadataReference> ExtractReferences(Assembly asm)
    {
        var refs = new List<MetadataReference>();
        if (!string.IsNullOrEmpty(asm.Location) && File.Exists(asm.Location))
        {
            refs.Add(MetadataReference.CreateFromFile(asm.Location));
        }

        foreach (var refAsmName in asm.GetReferencedAssemblies())
        {
            Compilation.TempContext tctx = new();
            try
            {
                var refAsm = tctx.LoadFromAssemblyName(refAsmName);
                if (!refAsm.IsDynamic && !string.IsNullOrEmpty(refAsm.Location))
                    refs.Add(MetadataReference.CreateFromFile(refAsm.Location));
            }
            catch { }
            tctx.FullUnload();
        }

        return refs;
    }
    
    public static Assembly? GetLoadedAssembly(string name)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(asm => asm.GetName().Name?.Equals(name, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    public static AssemblyDefinition? GetDefinitionFrom(Assembly asm)
    {
        if (asm.IsDynamic || string.IsNullOrEmpty(asm.Location)) return null;
        
        return AssemblyDefinition.ReadAssembly(asm.Location);
    }
}