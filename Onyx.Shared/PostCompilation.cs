using System.Reflection;
using Microsoft.CodeAnalysis;
using Mono.Cecil;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Utils;
using Onyx.Shared;

namespace Onyx.Shared;

public static class PostCompilation
{
    public static TypeDefinition ImportType(TypeDefinition type, ModuleDefinition target, bool add = true)
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
            
            foreach (var genParam in method.GenericParameters)
            {
                var newGenParam = new GenericParameter(genParam.Name, newMethod);
                newMethod.GenericParameters.Add(newGenParam);
            }
            
            newMethod.CustomAttributes.AddRange(method.CustomAttributes);

            if (method.HasBody)
            {
                foreach (var variable in method.Body.Variables)
                    newMethod.Body.Variables.Add(new VariableDefinition(target.ImportReference(variable.VariableType)));

                var ilProcessor = newMethod.Body.GetILProcessor();
                foreach (Instruction newInstruction in method.Body.Instructions.Select(instruction => CloneInstruction(instruction, target)))
                    ilProcessor.Append(newInstruction);
                
                Dictionary<Instruction, Instruction> instructionMap = [];
                for (int i = 0; i < method.Body.Instructions.Count; i++)
                    instructionMap[method.Body.Instructions[i]] = newMethod.Body.Instructions[i];

                foreach (var instruction in newMethod.Body.Instructions)
                {
                    if (instruction.Operand is Instruction targetInstruction)
                        instruction.Operand = instructionMap[targetInstruction];
                    if (instruction.Operand is Instruction[] targetInstructions)
                        instruction.Operand = targetInstructions.Select(targetInst => instructionMap[targetInst]).ToArray();
                    if (instruction.Operand is VariableDefinition targetVariable)
                        instruction.Operand = newMethod.Body.Variables[method.Body.Variables.IndexOf(targetVariable)];
                    if (instruction.Operand is ParameterDefinition targetParameter)
                        instruction.Operand = newMethod.Parameters[method.Parameters.IndexOf(targetParameter)];
                    if (instruction.Operand is MethodReference targetMethod)
                        instruction.Operand = target.ImportReference(targetMethod);
                }
                
                foreach (var handler in method.Body.ExceptionHandlers)
                {
                    newMethod.Body.ExceptionHandlers.Add(new ExceptionHandler(handler.HandlerType)
                    {
                        TryStart = newMethod.Body.Instructions[method.Body.Instructions.IndexOf(handler.TryStart)],
                        TryEnd = newMethod.Body.Instructions[method.Body.Instructions.IndexOf(handler.TryEnd)],
                        HandlerStart = newMethod.Body.Instructions[method.Body.Instructions.IndexOf(handler.HandlerStart)],
                        HandlerEnd = handler.HandlerEnd != null ? newMethod.Body.Instructions[method.Body.Instructions.IndexOf(handler.HandlerEnd)] : null,
                        CatchType = handler.CatchType != null ? target.ImportReference(handler.CatchType) : null,
                        FilterStart = handler.FilterStart != null ? newMethod.Body.Instructions[method.Body.Instructions.IndexOf(handler.FilterStart)] : null
                    });
                }
                
                newMethod.Body.InitLocals = method.Body.InitLocals;
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
                newProperty.GetMethod = newType.Methods.FirstOrDefault(m => m.Name == property.GetMethod.Name);
            }
            if (property.SetMethod != null)
            {
                newProperty.SetMethod = newType.Methods.FirstOrDefault(m => m.Name == property.SetMethod.Name);
            }
    
            newType.Properties.Add(newProperty);
        }
        
        foreach (var evt in type.Events)
        {
            var newEvent = new EventDefinition(
                evt.Name,
                evt.Attributes,
                target.ImportReference(evt.EventType));
    
            if (evt.AddMethod != null) newEvent.AddMethod = newType.Methods.FirstOrDefault(m => m.Name == evt.AddMethod.Name);
            if (evt.RemoveMethod != null) newEvent.RemoveMethod = newType.Methods.FirstOrDefault(m => m.Name == evt.RemoveMethod.Name);
            if (evt.InvokeMethod != null) newEvent.InvokeMethod = newType.Methods.FirstOrDefault(m => m.Name == evt.InvokeMethod.Name);
            foreach (var attr in evt.CustomAttributes) newEvent.CustomAttributes.Add(attr);
    
            newType.Events.Add(newEvent);
        }
        
        foreach (TypeDefinition importedNestedType in type.NestedTypes.Select(nestedType => ImportType(nestedType, target, add: false)))
        {
            newType.NestedTypes.Add(importedNestedType);
        }
        
        newType.BaseType = type.BaseType;
        newType.Attributes = type.Attributes;
        newType.CustomAttributes.AddRange(type.CustomAttributes);
        newType.Interfaces.AddRange(type.Interfaces);
        
        if (add) target.Types.Add(newType);

        return newType;
    }

    public static Instruction CloneInstruction(Instruction instruction, ModuleDefinition target)
    {
        var newInst = Instruction.Create(OpCodes.Nop);
        newInst.OpCode = instruction.OpCode;

        if (instruction.Operand is null) return Instruction.Create(instruction.OpCode);
        if (instruction.Operand is TypeReference typeRef) return Instruction.Create(instruction.OpCode, target.ImportReference(typeRef));
        if (instruction.Operand is MethodReference methodRef) return Instruction.Create(instruction.OpCode, target.ImportReference(methodRef));
        if (instruction.Operand is FieldReference fieldRef) return Instruction.Create(instruction.OpCode, target.ImportReference(fieldRef));
        if (instruction.Operand is string str) return Instruction.Create(instruction.OpCode, str);
        if (instruction.Operand is int i) return Instruction.Create(instruction.OpCode, i);
        if (instruction.Operand is long l) return Instruction.Create(instruction.OpCode, l);
        if (instruction.Operand is float f) return Instruction.Create(instruction.OpCode, f);
        if (instruction.Operand is double d) return Instruction.Create(instruction.OpCode, d);
        if (instruction.Operand is byte b) return Instruction.Create(instruction.OpCode, b);
        if (instruction.Operand is sbyte sb) return Instruction.Create(instruction.OpCode, sb);
        if (instruction.Operand is short s) return Instruction.Create(instruction.OpCode, s);
        if (instruction.Operand is ushort us) return Instruction.Create(instruction.OpCode, us);
        if (instruction.Operand is uint ui) return Instruction.Create(instruction.OpCode, ui);
        if (instruction.Operand is ulong ul) return Instruction.Create(instruction.OpCode, ul);
        if (instruction.Operand is Instruction targetInstruction) return Instruction.Create(instruction.OpCode, targetInstruction);
        if (instruction.Operand is Instruction[] targetInstructions) return Instruction.Create(instruction.OpCode, targetInstructions);
        if (instruction.Operand is VariableDefinition varDef) return Instruction.Create(instruction.OpCode, varDef);
        if (instruction.Operand is ParameterDefinition paramDef) return Instruction.Create(instruction.OpCode, paramDef);
        throw new Exception("Unknown operand type: " + instruction.Operand.GetType());
    }

    public static void Merge(string targetPath, List<string> dependencyPaths, string outputPath, bool obfuscateTarget = false)
    {
        var targetAsm = AssemblyDefinition.ReadAssembly(targetPath);
        HashSet<string> importedTypeNames = [];
        HashSet<string> mergedAssemblyNames = [];

        foreach (var depPath in dependencyPaths)
        {
            var depAsm = AssemblyDefinition.ReadAssembly(depPath);
            mergedAssemblyNames.Add(depAsm.Name.Name);
            ObfuscateAll(depAsm);
            foreach (var type in depAsm.MainModule.GetTypes())
            {
                if (ShouldSkipType(type)) continue;
                string name = $"{type.Namespace}.{type.Name}";
                if (importedTypeNames.Contains(name)) continue;
                ImportType(type, targetAsm.MainModule, add: true);
                importedTypeNames.Add(name);
            }
        }
        
        if (obfuscateTarget) ObfuscateAll(targetAsm);
        
        RewriteReferences(targetAsm, mergedAssemblyNames);
        targetAsm.Write(outputPath);
    }

    private static bool ShouldSkipType(TypeDefinition type)
    {
        if (type.Name == "<Module>") return true;
    
        if (type.Namespace?.StartsWith("System") == true) return true;
        if (type.Namespace?.StartsWith("Microsoft") == true) return true;

        return false;
    }

    public static void RewriteReferences(AssemblyDefinition assembly, HashSet<string> mergedAssemblyNames)
    {
        Dictionary<string, TypeDefinition> typeMap = [];
        foreach (var type in assembly.MainModule.GetTypes()) typeMap.TryAdd(type.FullName, type);

        foreach (var type in assembly.MainModule.GetTypes())
        {
            foreach (MethodDefinition method in type.Methods.Where(method => method.HasBody))
            {
                foreach (var instruction in method.Body.Instructions)
                {
                    switch (instruction.Operand)
                    {
                        case TypeReference typeRef:
                        {
                            if (ShouldRewrite(typeRef, mergedAssemblyNames)) instruction.Operand = RewriteTypeReference(typeRef, typeMap, assembly.MainModule);
                            break;
                        }
                        case MethodReference methodRef:
                        {
                            if (ShouldRewrite(methodRef.DeclaringType, mergedAssemblyNames)) instruction.Operand = RewriteMethodReference(methodRef, typeMap, assembly.MainModule);
                            break;
                        }
                        case FieldReference fieldRef:
                        {
                            if (ShouldRewrite(fieldRef.DeclaringType, mergedAssemblyNames)) instruction.Operand = RewriteFieldReference(fieldRef, typeMap, assembly.MainModule);
                            break;
                        }
                    }
                }

                foreach (var variable in method.Body.Variables)
                {
                    if (variable.VariableType is not { } typeRef || !ShouldRewrite(typeRef, mergedAssemblyNames)) continue;
                    var rewritten = RewriteTypeReference(typeRef, typeMap, assembly.MainModule);
                    variable.VariableType = rewritten;
                }
            }

            foreach (var field in type.Fields)
            {
                if (field.FieldType is not { } typeRef || !ShouldRewrite(typeRef, mergedAssemblyNames)) continue;
                var rewritten = RewriteTypeReference(typeRef, typeMap, assembly.MainModule);
                field.FieldType = rewritten;
            }
        }
    }

    private static bool ShouldRewrite(TypeReference typeRef, HashSet<string> mergedAssemblyNames) => 
        typeRef.Scope is AssemblyNameReference asmRef && mergedAssemblyNames.Contains(asmRef.Name);

    private static TypeReference RewriteTypeReference(TypeReference typeRef, Dictionary<string, TypeDefinition> typeMap, ModuleDefinition target)
    {
        if (typeRef is GenericInstanceType genericInstance)
        {
            var newGeneric = new GenericInstanceType(RewriteTypeReference(genericInstance.ElementType, typeMap, target));
            foreach (var genericArgument in genericInstance.GenericArguments)
                newGeneric.GenericArguments.Add(RewriteTypeReference(genericArgument, typeMap, target));
            return target.ImportReference(newGeneric);
        }
        if (typeRef is ArrayType arrayType) return new ArrayType(RewriteTypeReference(arrayType.ElementType, typeMap, target));
        if (typeRef is ByReferenceType byRefType) return new ByReferenceType(RewriteTypeReference(byRefType.ElementType, typeMap, target));
        if (typeRef is PointerType pointerType) return new PointerType(RewriteTypeReference(pointerType.ElementType, typeMap, target));
        if (typeMap.TryGetValue(typeRef.FullName, out var typeDef)) return target.ImportReference(typeDef);
        return target.ImportReference(typeRef);
    }

    private static MethodReference RewriteMethodReference(MethodReference methodRef, Dictionary<string, TypeDefinition> typeMap, ModuleDefinition target)
    {
        if (!typeMap.TryGetValue(methodRef.DeclaringType.FullName, out var typeDef)) return target.ImportReference(methodRef);
        var matchingMethod = typeDef.Methods.FirstOrDefault(m => 
            m.Name == methodRef.Name && 
            MethodSignaturesMatch(m, methodRef));
        
        if (matchingMethod is null) return target.ImportReference(methodRef);

        if (methodRef is GenericInstanceMethod genericMethod)
        {
            var newGenericMethod = new GenericInstanceMethod(target.ImportReference(matchingMethod));
            foreach (var genericArgument in genericMethod.GenericArguments)
                newGenericMethod.GenericArguments.Add(RewriteTypeReference(genericArgument, typeMap, target));
            return newGenericMethod;
        } 
        
        return target.ImportReference(matchingMethod);
    }

    private static FieldReference RewriteFieldReference(FieldReference fieldRef, Dictionary<string, TypeDefinition> typeMap, ModuleDefinition target)
    {
        if (!typeMap.TryGetValue(fieldRef.DeclaringType.FullName, out var typeDef)) return target.ImportReference(fieldRef);
        var matchingField = typeDef.Fields.FirstOrDefault(f => f.Name == fieldRef.Name);
        if (matchingField is null) return target.ImportReference(fieldRef);
        return target.ImportReference(matchingField);
    }

    private static bool MethodSignaturesMatch(MethodDefinition method, MethodReference methodRef)
    {
        if (method.Parameters.Count != methodRef.Parameters.Count) return false;
        if (method.Parameters.Where((t, i) => t.ParameterType.FullName != methodRef.Parameters[i].ParameterType.FullName).Any())
            return false;
        return method.ReturnType.FullName == methodRef.ReturnType.FullName;
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

    #region Obfuscation
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
    
    public static Dictionary<string, string> ObfuscateAll(AssemblyDefinition asm,
        Func<string, string>? obfuscateName = null)
    {
        if (obfuscateName == null)
            obfuscateName = name => GeneralUtilities.NewGUID(16, true);
        
        var allTypes = asm.MainModule.Types.ToList();
        var allMembers = asm.MainModule.Types.SelectMany(t => t.Methods.Cast<IMemberDefinition>())
            .Concat(asm.MainModule.Types.SelectMany(t => t.Fields.Cast<IMemberDefinition>()))
            .Concat(asm.MainModule.Types.SelectMany(t => t.Properties.Cast<IMemberDefinition>()))
            .Concat(asm.MainModule.Types.SelectMany(t => t.Events.Cast<IMemberDefinition>())).ToList();

        var obfuscatedNames = Obfuscate(allTypes.Cast<IMemberDefinition>().Concat(allMembers).ToList(), obfuscateName);
        
        foreach (var type in allTypes)
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
    #endregion
}