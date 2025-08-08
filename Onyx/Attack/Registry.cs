using System.Collections.Concurrent;
using System.Reflection;
using Onyx.Shared;
using static Onyx.Attack.Reflection;

namespace Onyx.Attack;

public partial class Registry
{
    public List<ConcurrentDictionary<int, Node>> Nodes { get; } = [];
    public ConcurrentDictionary<int, DateTime> Times { get; } = new();

    public Registry()
    {
        
    }

    public void Build(Node top, Action<Node, Exception>? onError = null, Func<Node, bool>? filter = null)
    {
        int time = top.Time;
        HashSet<int> visited = new();
        ConcurrentDictionary<int, Node> map = new();

        void Traverse(Node node)
        {
            if (!visited.Add(node.Id)) return;
            
            map[node.Id] = node;

            var refs = GetReferences(node, time);
            
            foreach (var reference in refs)
            {
                if (filter != null && !filter(reference)) continue;
                try { Traverse(reference); }
                catch (Exception ex) { onError?.Invoke(reference, ex); }
            }
        }
        
        Traverse(top);
        Nodes.Add(map);
        Times[time] = DateTime.Now;
    }
}