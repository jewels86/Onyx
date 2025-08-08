using Onyx.Attack;

namespace Crimson;

public partial class CrimsonModule
{
    public static void Recon()
    {
        // First, let's get a Registry started
        Registry registry = new();
        // We'll start with the whole AppDomain but we can filter out what we don't want
        var top = new Registry.AppDomainNode(AppDomain.CurrentDomain, 0);
        Func<Registry.Node, bool> filter = n =>
        {
            if (n is Registry.AssemblyNode asmNode)
            {
                // System assemblies are not interesting
                if (asmNode.Name.Contains("System") || asmNode.Name.Contains("Microsoft") || asmNode.Name.Contains("netstandard"))
                    return false;
            }
            return true;
        };

        registry.Build(top, filter: filter);
        // The registry now has a snapshot of everything reachable from the AppDomain
        // This will be helpful later
        
    }
}