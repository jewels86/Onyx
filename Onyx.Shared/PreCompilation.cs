using Mono.Cecil;

namespace Onyx.Shared;

public static class PreCompilation
{
    public static AssemblyDefinition FromPath(string path, ReaderParameters? parameters = null)
    {
        parameters ??= new ReaderParameters { ReadSymbols = false };
        return AssemblyDefinition.ReadAssembly(path, parameters);
    }
    
    public static AssemblyDefinition FromBytes(byte[] bytes, ReaderParameters? parameters = null)
    {
        parameters ??= new ReaderParameters { ReadSymbols = false };
        
        using var stream = new MemoryStream(bytes);
        return AssemblyDefinition.ReadAssembly(stream, parameters);
    }
    
    public static TypeDefinition TypeFromAsmDef(AssemblyDefinition asmDef, string typeName)
    {
        var type = asmDef.MainModule.GetType(typeName);
        if (type == null)
            throw new ArgumentException($"Type '{typeName}' not found in assembly '{asmDef.Name.Name}'.");
        return type;
    }
}