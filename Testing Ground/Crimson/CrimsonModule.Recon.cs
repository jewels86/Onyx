using Onyx.Attack;

namespace Crimson;

public partial class CrimsonModule
{
    public static void Recon()
    {
        // First, let's get a Registry started
        // We'll start with the whole AppDomain but we can filter out what we don't want
        Registry registry = new();
        var top = new Registry.AppDomainNode(AppDomain.CurrentDomain, 0);
        
        Func<Registry.Node, bool> filter = n =>
        {
            if (n is Registry.AssemblyNode asmNode)
            {
                // System assemblies are not interesting
                if (asmNode.Name.Contains("System") || asmNode.Name.Contains("netstandard"))
                    return false;
            }

            if (n is Registry.TypeNode typeNode)
            {
                if (typeNode.Type?.AssemblyQualifiedName?.Contains("System") == true) return false;
                if (typeNode.Type?.AssemblyQualifiedName?.Contains("Microsoft") == true) return false;
            }
            
            return true;
        };

        registry.Build(top, filter: filter);
        
        var builderType = Reflection.SearchForType("Microsoft.AspNetCore.Builder.WebApplicationBuilder");
        var appType = Reflection.SearchForType("Microsoft.AspNetCore.Builder.WebApplication");
        var builderNode = registry.GetInstancesOfType(builderType, 0).FirstOrDefault();
        var appNode = registry.GetInstancesOfType(appType, 0).FirstOrDefault();
        
        // Now we can use this to get some nice information
    }
}