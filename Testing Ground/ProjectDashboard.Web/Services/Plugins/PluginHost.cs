using System.Reflection;
using System.Runtime.Loader;
using ProjectDashboard.Abstractions;

namespace ProjectDashboard.Web.Services.Plugins;

public class PluginHost
{
    private readonly List<IPlugin> _plugins = new();
    public IReadOnlyList<IPlugin> Plugins => _plugins;

    public void LoadFromFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath)) return;
        foreach (var file in Directory.GetFiles(folderPath, "*.dll", SearchOption.TopDirectoryOnly))
        {
            try
            {
                Console.WriteLine($"[PluginHost] Loading plugin from: {file}");
                var asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(file));
                var types = asm.GetTypes().Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
                foreach (var t in types)
                {
                    if (Activator.CreateInstance(t) is IPlugin p)
                    {
                        Console.WriteLine($"[PluginHost] Loaded plugin: {p.Name}");
                        _plugins.Add(p);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PluginHost] Failed to load '{file}': {ex.Message}");
            }
        }
    }
}
