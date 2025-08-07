using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Onyx.Shared;
using static Onyx.Attack.Reflection;
using static Onyx.Shared.GeneralUtilities;

namespace Onyx.Attack;

public partial class Registry
{
    #region Classes
    public class Node
    {
        public int Id { get; }
        public string Name { get; set; }
        public List<Edge> Edges { get; }
        public int Time { get; }
        
        public Node(int id, string name, int time)
        {
            Id = id;
            Name = string.IsNullOrEmpty(name) ? name : string.Intern(name);
            Edges = [];
            Time = time;
        }
        
        public Node WithEdgeTo(Node to, EdgeType edgeType, string? label = null, bool bidirectional = false)
        {
            if (bidirectional)
                to.Edges.Add(new Edge(to, this, edgeType));
            Edges.Add(new Edge(this, to, edgeType));
            return this;
        }
        
        public Node WithEdgeFrom(Node from, EdgeType edgeType, string? label = null, bool bidirectional = false)
        {
            Edges.Add(new Edge(from, this, edgeType));
            if (bidirectional)
                from.Edges.Add(new Edge(this, from, edgeType));
            return this;
        }
        
        public List<Edge> DownstreamEdges() => Edges.Where(e => e.FromId == this.Id).ToList();
        public List<Edge> UpstreamEdges() => Edges.Where(e => e.ToId == this.Id).ToList();
    }

    public readonly struct Edge
    {
        public int ToId { get; }
        public int FromId { get; }
        public string Label => GetFriendlyEdgeLabel(EdgeType);
        public EdgeType EdgeType { get; }

        public Edge(Node to, Node from, EdgeType edgeType, string? label = null)
        {
            ToId = to.Id;
            FromId = from.Id;
            EdgeType = edgeType;
            // label parameter is ignored for visualization, Label is computed
        }

        public override string ToString() => $"{FromId} {Label} {ToId} ({EdgeType})";
    }
    
    public enum EdgeType 
    {
        AppDomainContainsAssembly,
        AssemblyContainsType,
        TypeHasInstance,
        InstanceHasInstance,
        InstanceHasType
    }

    public class InstanceWeakVariablePackage : WeakVariablePackage
    {
        public InstanceType InstanceType { get; set; }
        
        public InstanceWeakVariablePackage(string name, object? value, AccessModifier access, InstanceType instanceType, ReflectionResult result = ReflectionResult.Success) 
            : base(string.IsNullOrEmpty(name) ? name : string.Intern(name), value, access, result)
        {
            InstanceType = instanceType;
        }
    }

    [Flags]
    public enum InstanceType
    {
        None = 0,
        Field = 2, 
        Property = 4,
        Constant = 8,
        Static = 16,
        Irrelevant = 32
    }
    #endregion
    #region Node Implementations
    public class AppDomainNode : Node
    {
        public AppDomain AppDomain { get; }
        
        public AppDomainNode(int id, AppDomain appDomain, int time, string? name = null) 
            : base(id, string.IsNullOrEmpty(name ?? appDomain.FriendlyName) ? (name ?? appDomain.FriendlyName) : string.Intern(name ?? appDomain.FriendlyName), time)
        {
            AppDomain = appDomain;
        }
        
        public override string ToString() => $"(AppDomain) {Name}";
    }
    
    public class AssemblyNode : Node
    {
        public WeakReference<Assembly> Reference { get; }
        public Assembly? Assembly => Reference.TryGetTarget(out var asm) ? asm : null;

        public AssemblyNode(int id, Assembly assembly, int time, string? name = null) 
            : base(id, string.IsNullOrEmpty(name ?? assembly.FullName ?? "unknown_asm_" + NewGUID(8, true)) ? (name ?? assembly.FullName ?? "unknown_asm_" + NewGUID(8, true)) : string.Intern(name ?? assembly.FullName ?? "unknown_asm_" + NewGUID(8, true)), time)
        {
            Reference = new(assembly);
        }

        public override string ToString() => $"(Assembly) {Name}";
    }
    
    public class TypeNode : Node
    {
        public WeakReference<Type> Reference { get; }
        public Type? Type => Reference.TryGetTarget(out var type) ? type : null;

        public TypeNode(int id, Type type, int time, string? name = null) 
            : base(id, string.IsNullOrEmpty(name ?? type.FullName ?? "unknown_type_" + NewGUID(8, true)) ? (name ?? type.FullName ?? "unknown_type_" + NewGUID(8, true)) : string.Intern(name ?? type.FullName ?? "unknown_type_" + NewGUID(8, true)), time)
        {
            Reference = new(type);
        }

        public override string ToString() => $"(Type) {Name}";
    }

    public class InstanceNode : Node
    {
        public WeakReference<object> Instance { get; }
        public Type Type { get; }
        public InstanceType InstanceType { get; set; }
        
        public InstanceNode(int id, object instance, int time, InstanceType instanceType, string? name = null, Type? type = null) 
            : base(id, string.IsNullOrEmpty(name ?? instance.GetType().FullName ?? "unknown_instance_" + NewGUID(8, true)) ? (name ?? instance.GetType().FullName ?? "unknown_instance_" + NewGUID(8, true)) : string.Intern(name ?? instance.GetType().FullName ?? "unknown_instance_" + NewGUID(8, true)), time)
        {
            Instance = new(instance);
            Type = type ?? instance.GetType();
            InstanceType = instanceType;
        }
        
        public override string ToString() => $"(Instance) {InstanceTypeToLabel(InstanceType)} {Name} : {Type.FullName}";
    }
    #endregion
    #region References
    public static List<Assembly> GetReferences(AppDomain appDomain)
    {
        return appDomain.GetAssemblies().ToList();
    }
    
    public static List<Type> GetReferences(Assembly asm)
    {
        Type[] types;
        try { types = asm.GetTypes(); } 
        catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null).ToArray()!; }

        return types.ToList();
    }

    public static List<InstanceWeakVariablePackage> GetReferences(Type type)
    {
        List<InstanceWeakVariablePackage> instances = GetAllFields(type)
            .Select(x => ((IVariablePackage)x, InstanceType.Field))
            .Concat(GetAllProperties(type).Select(x => ((IVariablePackage)x, InstanceType.Property)))
            .Select(x => 
                new InstanceWeakVariablePackage(x.Item1.Name, x.Item1.Value, x.Item1.Access, x.Item2, x.Item1.Result)).ToList();
        return instances;
    }

    public static List<WeakVariablePackage> GetReferences(object instance)
    {
        List<InstanceWeakVariablePackage> packages = WithInstanceType(Reflection.GetAllFields(instance), InstanceType.Field)
            .Concat(WithInstanceType(Reflection.GetAllProperties(instance), InstanceType.Property))
            .Select(x => 
                new InstanceWeakVariablePackage(x.Item1.Name, x.Item1.Value, x.Item1.Access, x.Item2, x.Item1.Result)).ToList();
        return packages.Select(x => new WeakVariablePackage(x.Name, x.Value, x.Access, x.Result)).ToList();
    }

    public static List<Node> GetReferences(Node node, int time)
    {
        if (node is AppDomainNode appDomainNode)
        {
            return GetReferences(appDomainNode.AppDomain)
                .Select(x => new AssemblyNode(TryGetHashCode(x), x, time, x.FullName)
                    .WithEdgeFrom(appDomainNode, EdgeType.AppDomainContainsAssembly, "contains", true))
                .ToList();
        }
        if (node is AssemblyNode asmNode)
        {
            return GetReferences(asmNode.Assembly)
                .Select(x => new TypeNode(TryGetHashCode(x), x, time, GetCSharpTypeName(x))
                    .WithEdgeFrom(asmNode, EdgeType.AssemblyContainsType, "contains", true))
                .ToList();
        }

        if (node is TypeNode typeNode)
        {
            if (typeNode.Type is null) return [];
            return GetReferences(typeNode.Type)
                .Where(x => x.Value != null)
                .Select(x =>
                {
                    object? val = x.Value;
                    if (val is null) return null;
                    return new InstanceNode(TryGetHashCode(val), val, time, x.InstanceType, x.Name, typeNode.Type)
                        .WithEdgeFrom(typeNode, EdgeType.TypeHasInstance, "has instance", true);
                })
                .Where(x => x != null)
                .ToList()!;
        } 
        if (node is InstanceNode instanceNode)
        {
            if (!instanceNode.Instance.TryGetTarget(out var inst)) return [];
            return GetReferences(inst)
                .Where(x => x.Value != null)
                .Select(x =>
                {
                    object? val = x.Value;
                    if (val is null) return null;
                    return new InstanceNode(TryGetHashCode(val), val, time, InstanceType.Field, x.Name, instanceNode.Type)
                        .WithEdgeFrom(instanceNode, EdgeType.InstanceHasInstance, "has instance", true);
                })
                .Where(x => x != null)
                .ToList()!;
        }
        return [];
    }
    #endregion
    #region Utilities
    public static string InstanceTypeToLabel(InstanceType type)
    {
        StringBuilder sb = new();
        if (type.HasFlag(InstanceType.Constant)) sb.Append("constant/default ");
        if (type.HasFlag(InstanceType.Static)) sb.Append("static ");
        if (type.HasFlag(InstanceType.Field)) sb.Append("field ");
        if (type.HasFlag(InstanceType.Property)) sb.Append("property ");
        return sb.ToString().Trim();
    }

    public static IEnumerable<(IVariablePackage, InstanceType)> WithInstanceType(IEnumerable<IVariablePackage> packages, InstanceType type)
    {
        return packages.Select(x => (x, type));
    }

    /// <summary>
    /// Prints the graph starting from the given node using BFS.
    /// </summary>
    /// <param name="node">The node to print from.</param>
    /// <param name="getEdges">A function specifying which edges to use from a given node.</param>
    /// <param name="depthLimit">The recursion limit on printing.</param>
    /// <remarks><b>DO NOT</b> use this with <see cref="GetRoot"/> unless your tree is controlled.</remarks>
    public void PrintGraph(Node node, Func<Node, List<Edge>>? getEdges = null, int depthLimit = -1)
    {
        HashSet<int> visited = new();
        Queue<(Node, int)> queue = new();
        queue.Enqueue((node, 0));
        if (getEdges is null) getEdges = n => n.Edges;

        while (queue.Count > 0)
        {
            var (current, depth) = queue.Dequeue();
            if (!visited.Add(current.Id)) continue;

            Console.WriteLine($"{new string(' ', depth * 2)}- {current}");

            if (depthLimit == -1 || depth < depthLimit)
            {
                foreach (var edge in getEdges(current))
                {
                    var targetNode = Nodes[current.Time].TryGetValue(edge.ToId, out var n) ? n : null;
                    if (targetNode != null)
                    {
                        Console.WriteLine($"{new string(' ', (depth + 1) * 2)}-> {targetNode} [{edge.Label}] ({edge.EdgeType})");
                        if (!visited.Contains(edge.ToId))
                            queue.Enqueue((targetNode, depth + 1));
                    }
                }
            }
        }
    }
    
    public Node? GetRoot(int time)
    {
        try
        {
            return Nodes[time].Where(x => x.Value is AppDomainNode).Select(x => x.Value).FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    public static int TryGetHashCode(object? obj)
    {
        try { return obj?.GetHashCode() ?? -1; }
        catch { return -1; }
    }

    public Node? Get(object obj, int time)
    {
        return GeneralUtilities.TryCatch(() => Nodes[time][TryGetHashCode(obj)], null, null);
    }

    public static string GetFriendlyEdgeLabel(EdgeType type)
    {
        return type switch
        {
            EdgeType.AppDomainContainsAssembly => "contains assembly",
            EdgeType.AssemblyContainsType => "contains type",
            EdgeType.TypeHasInstance => "has instance",
            EdgeType.InstanceHasInstance => "has instance",
            EdgeType.InstanceHasType => "is of type",
            _ => "related to"
        };
    }
    #endregion
}