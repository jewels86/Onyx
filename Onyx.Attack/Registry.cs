using System.Collections.Concurrent;
using System.Reflection;
using Onyx.Shared;
using static Onyx.Shared.Reflection;

namespace Onyx.Attack;

public partial class Registry
{
    public List<ConcurrentDictionary<int, Node>> Nodes { get; } = [];
    public List<ConcurrentDictionary<WeakType, List<InstanceNode>>> NodesOfType { get; } = [];
    public ConcurrentDictionary<int, DateTime> Times { get; } = new();

    public void Build(Node top, Func<Node, bool>? filter = null, Action<Node, Exception>? onError = null, int depthLimit = -1)
    {
        int time = top.Time;
        HashSet<int> visited = new();
        ConcurrentDictionary<int, Node> map = new();

        void Traverse(Node node, int depth)
        {
            if (!visited.Add(node.Id)) return;
            map[node.Id] = node;
            if (node is InstanceNode instance && instance.Type != null) 
                NodesOfType[time].GetOrAdd(new(instance.Type), _ => []).Add(instance);

            if (depthLimit != -1 && depth >= depthLimit) return;

            var refs = GetReferences(node, time);
            foreach (var reference in refs)
            {
                if (filter != null && !filter(reference)) continue;
                try { Traverse(reference, depth + 1); }
                catch (Exception ex) { onError?.Invoke(reference, ex); }
            }
        }
        
        Traverse(top, 0);
        Nodes.Add(map);
        Times[time] = DateTime.Now;
    }
}