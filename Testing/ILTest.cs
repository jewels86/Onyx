using Onyx.Attack;

namespace Testing;

public class ILTest
{
    public static async Task Run()
    {
        object? result = await Compilation.Run("1 + 1");
        Console.WriteLine(result);

        int x = 1;
        int y = 2;
        Console.WriteLine(Reflection.FromObject(() => x).Name);
        Console.WriteLine(await Compilation.ICC("return x + y;", [() => x, () => y])());
    }
}