using Onyx.Shared;

namespace Testing;

public class CompilationTest
{
    public static async Task Run()
    {
        object? result = await Compilation.Run("1 + 1");
        Console.WriteLine(result);

        int x = 1;
        int y = 2;
        Console.WriteLine(Reflection.FromObject(() => x).Name);
        Console.WriteLine(await Compilation.ICC("return x + y;", [() => x, () => y])());
        
        var code = @"
            using System;
            public class MyClass
            {
                public int X { get; set; }
                public MyClass(int x) { X = x; }
                public void PrintX() { Console.WriteLine(X); }
            }";
        int result2 = Compilation.CompileAndUseTyped<int>(code, (asm) =>
        {
            Type t = asm.GetType("MyClass") ?? throw new Exception("Type MyClass not found");
            dynamic myClass = Activator.CreateInstance(t, 12) ?? throw new Exception("Failed to create MyClass instance");
            myClass.PrintX();
            return myClass.X;
        });
        Console.WriteLine(result2);
    }
}