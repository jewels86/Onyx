using System.Reflection;
using Onyx.Attack;

namespace Testing;

public static class RegistryTest
{
    public static void Run()
    {
        Registry registry = new();
        var top = new Registry.AppDomainNode(AppDomain.CurrentDomain, 0);
        registry.Build(top, filter: new Registry.Filter()
            .WithAssembly(Assembly.GetExecutingAssembly())
            .WithAllowOtherAssemblies(false)
            .WithAssembliesApplyToTypes(true)
            .WithAssembliesApplyToInstances(true)
            .WithFinish(), depthLimit: 10);
        registry.PrintGraph(top);
    }
}