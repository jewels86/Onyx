using Onyx.Shared;

namespace Onyx.Attack.PostCompilation;

public class Program
{
    public static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: Onyx.Attack.PostCompilation <targetPath>");
            return;
        }
        string targetPath = args[0];
        var assembly = Mono.Cecil.AssemblyDefinition.ReadAssembly(targetPath);
        
        Console.WriteLine($"Obfuscating all types and members with attribute [ObfuscateAttribute] in {assembly.Name.Name} {assembly.Name.Version}");
        
        Shared.PostCompilation.ObfuscateWithAttribute(assembly);
    }
}