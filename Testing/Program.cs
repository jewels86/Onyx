using System.Reflection;
using System.Reflection.Emit;
using Onyx.Attack;
using Onyx.Shared;

namespace Testing;

class Program
{
    static async Task Main(string[] args)
    {
        MyClassTest.Run();
        await CompilationTest.Run();
        RegistryTest.Run();
    }
}