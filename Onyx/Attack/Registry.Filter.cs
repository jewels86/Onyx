using System.Reflection;

namespace Onyx.Attack;

public partial class Registry
{
    #region Weak stuff

    public class Weak<T> where T : class
    {
        public WeakReference<T> Reference { get; }
        public int HashCode { get; }
        public T? Value => Reference.TryGetTarget(out var v) ? v : null;

        public Weak(T value)
        {
            Reference = new(value);
            HashCode = value.GetHashCode();
        }
    }

    public class WeakType : Weak<Type>
    {
        public WeakType(Type type) : base(type)
        {
        }
    }

    public class WeakAssembly : Weak<Assembly>
    {
        public WeakAssembly(Assembly assembly) : base(assembly)
        {
        }
    }

    #endregion

    public class Filter
    {
        #region Settings

        public List<Func<Node, bool>> Predicates { get; set; } = []; // main body of the filter
        public List<Func<Node, bool>> Passes { get; set; } = []; // take place at the beginning- overrides

        public bool AllowByDefault { get; set; } = true;

        public List<WeakType> WhitelistedTypes { get; } = [];
        public List<WeakType> BlacklistedTypes { get; } = [];
        public bool AllowOtherTypes { get; set; } = true;

        public List<WeakAssembly> WhitelistedAssemblies { get; } = [];
        public List<WeakAssembly> BlacklistedAssemblies { get; } = [];
        public bool AllowOtherAssemblies { get; set; } = true;
        public bool AssembliesApplyToTypes { get; set; } = true;
        public bool AssembliesApplyToInstances { get; set; } = false;
        public bool AssemblyContainsAppliesToTypes { get; set; } = false;
        public bool AssemblyContainsAppliesToInstances { get; set; } = false;
        public List<string> WhitelistedAssemblyContains { get; } = [];
        public List<string> BlacklistedAssemblyContains { get; } = [];
        

        public List<WeakType> WhitelistedInstanceTypes { get; } = [];
        public List<WeakType> BlacklistedInstanceTypes { get; } = [];
        public bool AllowOtherInstanceTypes { get; set; } = true;

        #endregion
        #region Constructors
        public Filter() {}

        public Filter(Filter filter)
        {
            Predicates = new(filter.Predicates);
            Passes = new(filter.Passes);
            
            AllowByDefault = filter.AllowByDefault;
            WhitelistedTypes = new(filter.WhitelistedTypes);
            BlacklistedTypes = new(filter.BlacklistedTypes);
            AllowOtherTypes = filter.AllowOtherTypes;
            WhitelistedAssemblies = new(filter.WhitelistedAssemblies);
            BlacklistedAssemblies = new(filter.BlacklistedAssemblies);
            AllowOtherAssemblies = filter.AllowOtherAssemblies;
            AssembliesApplyToTypes = filter.AssembliesApplyToTypes;
            AssembliesApplyToInstances = filter.AssembliesApplyToInstances;
            AssemblyContainsAppliesToTypes = filter.AssemblyContainsAppliesToTypes;
            AssemblyContainsAppliesToInstances = filter.AssemblyContainsAppliesToInstances;
            WhitelistedAssemblyContains = new(filter.WhitelistedAssemblyContains);
            BlacklistedAssemblyContains = new(filter.BlacklistedAssemblyContains);
            WhitelistedInstanceTypes = new(filter.WhitelistedInstanceTypes);
            BlacklistedInstanceTypes = new(filter.BlacklistedInstanceTypes);
            AllowOtherInstanceTypes = filter.AllowOtherInstanceTypes;
        }
        
        public Filter(
            bool allowByDefault = true,
            IEnumerable<Type>? whitelistedTypes = null,
            IEnumerable<Type>? blacklistedTypes = null,
            bool allowOtherTypes = true,
            IEnumerable<Assembly>? whitelistedAssemblies = null,
            IEnumerable<Assembly>? blacklistedAssemblies = null,
            bool allowOtherAssemblies = true,
            bool assembliesApplyToTypes = true,
            bool assembliesApplyToInstances = false,
            bool assemblyContainsAppliesToTypes = false,
            bool assemblyContainsAppliesToInstances = false,
            IEnumerable<string>? whitelistedAssemblyContains = null,
            IEnumerable<string>? blacklistedAssemblyContains = null,
            IEnumerable<Type>? whitelistedInstanceTypes = null,
            IEnumerable<Type>? blacklistedInstanceTypes = null,
            bool allowOtherInstanceTypes = true
        )
        {
            AllowByDefault = allowByDefault;
            if (whitelistedTypes != null) foreach (var t in whitelistedTypes) WhitelistedTypes.Add(new(t));
            if (blacklistedTypes != null) foreach (var t in blacklistedTypes) BlacklistedTypes.Add(new(t));
            AllowOtherTypes = allowOtherTypes;
            if (whitelistedAssemblies != null) foreach (var a in whitelistedAssemblies) WhitelistedAssemblies.Add(new(a));
            if (blacklistedAssemblies != null) foreach (var a in blacklistedAssemblies) BlacklistedAssemblies.Add(new(a));
            AllowOtherAssemblies = allowOtherAssemblies;
            AssembliesApplyToTypes = assembliesApplyToTypes;
            AssembliesApplyToInstances = assembliesApplyToInstances;
            AssemblyContainsAppliesToTypes = assemblyContainsAppliesToTypes;
            AssemblyContainsAppliesToInstances = assemblyContainsAppliesToInstances;
            if (whitelistedAssemblyContains != null) foreach (var s in whitelistedAssemblyContains) WhitelistedAssemblyContains.Add(s);
            if (blacklistedAssemblyContains != null) foreach (var s in blacklistedAssemblyContains) BlacklistedAssemblyContains.Add(s);
            if (whitelistedInstanceTypes != null) foreach (var t in whitelistedInstanceTypes) WhitelistedInstanceTypes.Add(new(t));
            if (blacklistedInstanceTypes != null) foreach (var t in blacklistedInstanceTypes) BlacklistedInstanceTypes.Add(new(t));
            AllowOtherInstanceTypes = allowOtherInstanceTypes;
        }
        #endregion
        
        #region Methods

        public Func<Node, bool> Function => n =>
        {
            foreach (var pass in Passes)
                if (pass(n))
                    return true;
            foreach (var pred in Predicates)
                if (!pred(n))
                    return false;
            return true;
        };

        public static implicit operator Func<Node, bool>(Filter filter)
        {
            return filter.Function;
        }

        public void Finish(Func<Node, bool>? customPredicates = null, Func<Node, bool>? customPasses = null)
        {
            List<Func<Node, bool>> predicates = [];
            List<Func<Node, bool>> passes = [];
            if (customPredicates != null) predicates.Add(customPredicates);
            if (customPasses != null) passes.Add(customPasses);

            foreach (var type in WhitelistedTypes) passes.Add(n => n is TypeNode tn && tn.Type == type.Value);
            foreach (var type in BlacklistedTypes) predicates.Add(n => !(n is TypeNode tn && tn.Type == type.Value));
            if (AllowOtherTypes) predicates.Add(n => n is TypeNode);

            foreach (var asm in WhitelistedAssemblies)
                passes.Add(n => n is AssemblyNode asmn && asmn.Assembly == asm.Value);
            foreach (var asm in BlacklistedAssemblies)
                predicates.Add(n => !(n is AssemblyNode asmn && asmn.Assembly == asm.Value));
            if (AllowOtherAssemblies) predicates.Add(n => n is AssemblyNode);
            if (AssembliesApplyToTypes)
            {
                foreach (var asm in WhitelistedAssemblies)
                    passes.Add(n => n is TypeNode tn && tn.Type?.Assembly == asm.Value);
                foreach (var asm in BlacklistedAssemblies)
                    predicates.Add(n => !(n is TypeNode tn && tn.Type?.Assembly == asm.Value));
            }

            if (AssembliesApplyToInstances)
            {
                foreach (var asm in WhitelistedAssemblies)
                    passes.Add(n => n is InstanceNode inst && inst.Type?.Assembly == asm.Value);
                foreach (var asm in BlacklistedAssemblies)
                    predicates.Add(n => !(n is InstanceNode inst && inst.Type?.Assembly == asm.Value));
            }
            if (AssemblyContainsAppliesToTypes)
            {
                foreach (var contains in WhitelistedAssemblyContains)
                    passes.Add(n => n is TypeNode tn && tn.Type?.AssemblyQualifiedName != null && tn.Type.AssemblyQualifiedName.Contains(contains));
                foreach (var contains in BlacklistedAssemblyContains)
                    predicates.Add(n => !(n is TypeNode tn && tn.Type?.AssemblyQualifiedName != null && tn.Type.AssemblyQualifiedName.Contains(contains)));
            }
            if (AssemblyContainsAppliesToInstances)
            {
                foreach (var contains in WhitelistedAssemblyContains)
                    passes.Add(n => n is InstanceNode inst && inst.Type?.AssemblyQualifiedName != null && inst.Type.AssemblyQualifiedName.Contains(contains));
                foreach (var contains in BlacklistedAssemblyContains)
                    predicates.Add(n => !(n is InstanceNode inst && inst.Type?.AssemblyQualifiedName != null && inst.Type.AssemblyQualifiedName.Contains(contains)));
            }
            foreach (var contains in WhitelistedAssemblyContains)
                passes.Add(n => n is AssemblyNode asmn && asmn.Assembly?.FullName != null && asmn.Assembly.FullName.Contains(contains));
            foreach (var contains in BlacklistedAssemblyContains)
                predicates.Add(n => !(n is AssemblyNode asmn && asmn.Assembly?.FullName != null && asmn.Assembly.FullName.Contains(contains)));
            
            foreach (var type in WhitelistedInstanceTypes)
                passes.Add(n => n is InstanceNode inst && inst.Type == type.Value);
            foreach (var type in BlacklistedInstanceTypes)
                predicates.Add(n => !(n is InstanceNode inst && inst.Type == type.Value));
            if (AllowOtherInstanceTypes) predicates.Add(n => n is InstanceNode);

            if (AllowByDefault) predicates.Add(n => true);
            else passes.Add(n => false);

            Predicates = predicates;
            Passes = passes;
        }
        #endregion
        #region Extensions
        public Filter WithAllowByDefault(bool value = true)
        {
            AllowByDefault = value;
            return this;
        }

        public Filter WithType(Type type, bool allow = true)
        {
            if (allow) WhitelistedTypes.Add(new(type));
            else BlacklistedTypes.Add(new(type));
            return this;
        }

        public Filter WithAllowOtherTypes(bool value = true)
        {
            AllowOtherTypes = value;
            return this;
        }

        public Filter WithAssembly(Assembly assembly, bool allow = true)
        {
            if (allow) WhitelistedAssemblies.Add(new(assembly));
            else BlacklistedAssemblies.Add(new(assembly));
            return this;
        }

        public Filter WithAllowOtherAssemblies(bool value = true)
        {
            AllowOtherAssemblies = value;
            return this;
        }

        public Filter WithAssembliesApplyToTypes(bool value = true)
        {
            AssembliesApplyToTypes = value;
            return this;
        }

        public Filter WithAssembliesApplyToInstances(bool value = false)
        {
            AssembliesApplyToInstances = value;
            return this;
        }

        public Filter WithAssemblyContainsAppliesToTypes(bool value = false)
        {
            AssemblyContainsAppliesToTypes = value;
            return this;
        }

        public Filter WithAssemblyContainsAppliesToInstances(bool value = false)
        {
            AssemblyContainsAppliesToInstances = value;
            return this;
        }

        public Filter WithInstanceType(Type type, bool allow = true)
        {
            if (allow) WhitelistedInstanceTypes.Add(new(type));
            else BlacklistedInstanceTypes.Add(new(type));
            return this;
        }

        public Filter WithAllowOtherInstanceTypes(bool value = true)
        {
            AllowOtherInstanceTypes = value;
            return this;
        }

        public Filter WithWhitelistedAssemblyContains(params string[] values)
        {
            foreach (var v in values) WhitelistedAssemblyContains.Add(v);
            return this;
        }

        public Filter WithBlacklistedAssemblyContains(params string[] values)
        {
            foreach (var v in values) BlacklistedAssemblyContains.Add(v);
            return this;
        }
        #endregion
    }
    
    public static Filter DefaultFilter => new()
    {
        AllowByDefault = true,
        AllowOtherTypes = true,
        AllowOtherAssemblies = true,
        AllowOtherInstanceTypes = true,
        AssembliesApplyToTypes = true,
        AssembliesApplyToInstances = false
    };
    
}