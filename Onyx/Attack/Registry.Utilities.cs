using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Onyx.Attack.Reflection;
using static Onyx.Shared.GeneralUtilities;

namespace Onyx.Attack;

public partial class Registry
{
    #region Classes
    public class Node
    {
        public int ID { get; }
        public string Name { get; set; }
        public List<Edge> Edges { get; }
        public int Time { get; }
        
        public Node(int id, string name, int time)
        {
            ID = id;
            Name = name;
            Edges = [];
            Time = time;
        }
    }

    public class Edge
    {
        public Node From { get; }
        public Node To { get; }
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
        public Type Type { get; }
        
        public TypeNode(int id, Type type, int time, string? name = null) 
            : base(id, name ?? type.FullName ?? "unknown_" + NewGUID(8, true), time)
        {
            Type = type;
        }
    }

    public class InstanceNode : Node
    {
        public WeakReference<object> Instance { get; }
        public Type Type { get; }
        
        public InstanceNode(int id, object instance, int time, string? name = null, Type? type = null) 
            : base(id, name ?? instance.GetType().FullName ?? "unknown_" + NewGUID(8, true), time)
        {
            Instance = new(instance);
            Type = type ?? instance.GetType();
        }
    }
    #endregion
    
}