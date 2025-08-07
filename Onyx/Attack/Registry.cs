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

    public void Build(Node node)
    {
        
    }
}