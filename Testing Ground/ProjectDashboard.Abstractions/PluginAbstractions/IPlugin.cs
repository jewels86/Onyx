namespace ProjectDashboard.Abstractions;

public interface IPlugin
{
    string Name { get; }
    string Description { get; }
    Task<PluginResult> ExecuteAsync(ProjectContext context, CancellationToken ct = default);
}

public sealed class PluginResult
{
    public bool Success { get; set; }
    public string Output { get; set; } = string.Empty;
}
