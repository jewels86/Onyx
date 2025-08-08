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
        
        Registry.Filter filter = new Registry.Filter(Registry.DefaultFilter)
            .With
        registry.Build(top, filter: filter);
        
        var builderType = Reflection.SearchForType("Microsoft.AspNetCore.Builder.WebApplicationBuilder");
        var appType = Reflection.SearchForType("Microsoft.AspNetCore.Builder.WebApplication");
        var builderNode = registry.GetInstancesOfType(builderType, 0).FirstOrDefault();
        var appNode = registry.GetInstancesOfType(appType, 0).FirstOrDefault();
        
        // Now we can use this to get some nice information
    }
}