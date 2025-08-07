using System.Collections.Concurrent;
using System.Reflection;
using Onyx.Shared;
using static Onyx.Attack.Reflection;

namespace Onyx.Attack;

public partial class Registry
{
    public List<ConcurrentDictionary<int, Node>> Nodes { get; } = [];
    public ConcurrentDictionary<int, DateTime> Times { get; } = new();
    public List<ConcurrentBag<Edge>> Edges { get; } = [];

    public Registry(AppDomain? appDomain = null, Action<Node, Exception>? onError = null)
    {
        appDomain ??= AppDomain.CurrentDomain;
        Build(new AppDomainNode(appDomain.GetHashCode(), appDomain, 0), onError);
    }

    public void Build(Node top, Action<Node, Exception>? onError = null)
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
                try { Traverse(reference); }
                catch (Exception ex) { onError?.Invoke(reference, ex); }
            }
        }
        
        Traverse(top);
        Nodes.Add(map);
        Times[time] = DateTime.Now;
        Edges.Add(new ConcurrentBag<Edge>(map.Values.SelectMany(n => n.Edges)));
    }
}