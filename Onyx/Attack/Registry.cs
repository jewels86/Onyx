using System.Collections.Concurrent;
using System.Reflection;
using Onyx.Shared;
using static Onyx.Attack.Reflection;

namespace Onyx.Attack;

public partial class Registry
{
    public ConcurrentDictionary<string, Assembly> Assemblies { get; } = [];
    public ConcurrentDictionary<string, WeakReference<Type>> Types { get; } = [];
    public ConcurrentDictionary<string, WeakVariablePackage> Objects { get; } = [];

    public Registry()
    {
        Scan();
    }

    public void Scan()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var asm in assemblies)
        {
            string asmName = asm.FullName ?? asm.GetName().Name ?? "<unknown>" + GeneralUtilities.NewGUID(8, true);
            if (!Assemblies.ContainsKey(asmName))
                Assemblies[asmName] = asm;
            Type[] foundTypes;
            try { foundTypes = asm.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { foundTypes = ex.Types.Where(t => t != null).ToArray()!; }

            Queue<object> toProcess = new();
            while (toProcess.Count > 0)
            {
                var o = toProcess.Dequeue();
                List<WeakVariablePackage> vars;
                List<Type> types;
                if (o is Type type)
                {
                    (vars, types) = Enumerate(type);
                }
                else if (o is object obj)
                {
                    (vars, types) = Enumerate(obj);
                }
                else continue;

                foreach (var v in vars)
                {
                    Console.WriteLine(v.Name);
                }
            }
        }
    }
}