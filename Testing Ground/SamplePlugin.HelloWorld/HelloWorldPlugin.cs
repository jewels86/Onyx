using ProjectDashboard.Abstractions;
using System.Text;

public class HelloWorldPlugin : IPlugin
{
    public string Name => "Hello World";
    public string Description => "Demonstrates reading project, tasks, and secrets.";

    public Task<PluginResult> ExecuteAsync(ProjectContext context, CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Project: {context.Project.Name} (Owner: {context.Project.Owner})");
        sb.AppendLine($"Status: {context.Project.Status}");
        sb.AppendLine("Tasks:");
        foreach (var t in context.Tasks)
        {
            sb.AppendLine($" - [{(t.Completed ? 'x' : ' ')}] {t.Title} (Due: {t.Due?.ToShortDateString() ?? "n/a"})");
        }
        sb.AppendLine("Secrets:");
        foreach (var s in context.Secrets)
        {
            // DISCLAIMER: in real apps, don't print secrets. This is for demo only.
            sb.AppendLine($" - {s.Kind} {s.Name}: {s.Value}");
        }

        context.Log?.Invoke("HelloWorldPlugin executed.");
        return Task.FromResult(new PluginResult
        {
            Success = true,
            Output = sb.ToString()
        });
    }
}
