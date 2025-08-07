using System.Collections.Concurrent;
using System.Reflection;
using Onyx.Shared;
using static Onyx.Attack.Reflection;

namespace Onyx.Attack;

public partial class Registry
{
    public List<ConcurrentDictionary<int, Node>> Nodes { get; } = [];
    public ConcurrentDictionary<int, DateTime> Times { get; } = new();
    
    public Registry() {}

    public void Scan()
    {
        ConcurrentDictionary<int, Node> graph = new();
        int time = Times.Select(x => x.Key).Order().First() + 1;
        
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var asm in assemblies)
        {
            var asmHashCode = asm.GetHashCode();
            graph[asmHashCode] = new(asmHashCode, asm.FullName ?? "unknown_" + GeneralUtilities.NewGUID(8, true), time);

            var asmTypes = asm.GetTypes();
            
        }
    }
}