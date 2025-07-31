using System.Reflection;
using System.Reflection.Emit;
using Onyx.Defense;
using Onyx.Attack;
using Onyx.Shared;

namespace Testing;

class Program
{
    static async Task Main(string[] args)
    {
        object? result = await IL.Run("1 + 1");
        Console.WriteLine(result);
        
        var (assembly, tempContext) = 
            IL.Compile(IL.StandardUsings + "public class NewClass { public int A = 1; public int B = 2; public NewClass() { } }", "assembly0");
        if (assembly is null) throw new IL.UnableToCompileException("Failed to compile the code.");
        Type newClassType = IL.FromAssembly(assembly, "NewClass");
        object? instance = ClassBuilder.New(newClassType, []);
        if (instance is null) throw new IL.UnableToCompileException("Failed to create an instance of the class.");
        Console.WriteLine(Reflection.GetField(instance, "A").Value);
        tempContext.Unload();

        int x = 1;
        int y = 2;
        Console.WriteLine(Reflection.FromObject(() => x).Name);
        object? result3 = IL.ICC("x + y", [() => x, () => y])();
        Console.WriteLine(result3);
    }
}