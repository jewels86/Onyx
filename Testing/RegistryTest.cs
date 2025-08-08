using Onyx.Attack;

namespace Testing;

public static class RegistryTest
{
    public static void Run()
    {
        Registry registry = new();
        var top = new Registry.AppDomainNode(AppDomain.CurrentDomain, 0);
        registry.Build(top);
        Console.WriteLine($"Registry built with {registry.Nodes[0].Count} snapshots.");
        Console.WriteLine($"MyClass definition downstream:");
        if (registry.Get(typeof(MyClass), 0) is Registry.TypeNode myClassNode) 
            registry.PrintGraph(myClassNode, n => n.UpstreamEdges(), 2);
    }
}