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

        int x = 1;
        int y = 2;
        Console.WriteLine(Reflection.FromObject(() => x).Name);
        {
            var (compiled, context) = IL.ICC("x + y", [() => x, () => y]);
            var result3 = await compiled();

            compiled = null!;
            Console.WriteLine(result3);
            context.FullUnload();
        }
    }
}