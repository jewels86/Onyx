using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
            Name = name;
            Edges = [];
            Time = time;
        }
        
        public Node WithEdgeTo(Node to, EdgeType edgeType, string label)
        {
            Edges.Add(new Edge(to, this, edgeType, label));
            return this;
        }
        
        public Node WithEdgeFrom(Node from, EdgeType edgeType, string label)
        {
            Edges.Add(new Edge(this, from, edgeType, label));
            return this;
        }
    }

    public class Edge
    {
        public Node To { get; }
        public Node From { get; }
        public string Label { get; }
        public EdgeType EdgeType { get; }
        
        public Edge(Node to, Node from, EdgeType edgeType, string label)
        {
            To = to;
            From = from;
            EdgeType = edgeType;
            Label = label;
        }
    }
    
    public enum EdgeType 
    {
        AssemblyContainsType,
        TypeHasInstance,
        InstanceHasInstance
    }

    public class InstanceWeakVariablePackage : WeakVariablePackage
    {
        public InstanceType InstanceType { get; set; }
        
        public InstanceWeakVariablePackage(string name, object? value, AccessModifier access, InstanceType instanceType, ReflectionResult result = ReflectionResult.Success) 
            : base(name, value, access, result)
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
    public class AssemblyNode : Node
    {
        public Assembly Assembly { get; }
        
        public AssemblyNode(int id, Assembly assembly, int time, string? name = null) 
            : base(id, name ?? assembly.FullName ?? "unknown_" + NewGUID(8, true), time)
        {
            Assembly = assembly;
        }
    }
    
    public class TypeNode : Node
    {
        public WeakReference<Type> Reference { get; }
        public Type? Type => Reference.TryGetTarget(out var type) ? type : null;
        
        public TypeNode(int id, Type type, int time, string? name = null) 
            : base(id, name ?? type.FullName ?? "unknown_" + NewGUID(8, true), time)
        {
            Reference = new(type);
        }
    }

    public class InstanceNode : Node
    {
        public WeakReference<object> Instance { get; }
        public Type Type { get; }
        public InstanceType InstanceType { get; set; }
        
        public InstanceNode(int id, object instance, int time, InstanceType instanceType, string? name = null, Type? type = null) 
            : base(id, name ?? instance.GetType().FullName ?? "unknown_" + NewGUID(8, true), time)
        {
            Instance = new(instance);
            Type = type ?? instance.GetType();
            InstanceType = instanceType;
        }
    }
    #endregion
    #region References
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
        if (node is AssemblyNode asmNode)
        {
            return GetReferences(asmNode.Assembly)
                .Select(x => new TypeNode(x.GetHashCode(), x, time, GetCSharpTypeName(x))
                    .WithEdgeFrom(asmNode, EdgeType.AssemblyContainsType, "contains")).ToList();
        }

        if (node is TypeNode typeNode)
        {
            if (typeNode.Type is null) return [];
            return GetReferences(typeNode.Type)
                .Select(x =>
                {
                    object? val = x.Value;
                    if (val is null) return null;
                    return new InstanceNode(val.GetHashCode(), val, time, x.InstanceType, x.Name, typeNode.Type)
                        .WithEdgeFrom(typeNode, EdgeType.TypeHasInstance, "has instance");
                }).Where(x => x != null).ToList()!;
        } 
        if (node is InstanceNode instanceNode)
        {
            if (!instanceNode.Instance.TryGetTarget(out var inst)) return [];
            return GetReferences(inst)
                .Select(x =>
                {
                    object? val = x.Value;
                    if (val is null) return null;
                    return new InstanceNode(val.GetHashCode(), val, time, InstanceType.Field, x.Name, instanceNode.Type)
                        .WithEdgeFrom(instanceNode, EdgeType.InstanceHasInstance, "has instance");
                }).Where(x => x != null).ToList()!;
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
    #endregion
}