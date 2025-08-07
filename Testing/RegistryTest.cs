using Onyx.Attack;

namespace Testing;

public static class RegistryTest
{
    public static void Run()
    {
        Registry registry = new(onError: (node, ex) => Console.WriteLine($"Problem in {node}: {ex}"));
        registry.PrintGraph(registry.GetRoot(0) ?? throw new Exception("No root node found."));
        registry.GetRoot(0)?.Edges.ToList().ForEach(x => Console.WriteLine(x.ToString()));
    }
}