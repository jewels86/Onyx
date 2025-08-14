namespace ProjectDashboard.Abstractions;

public sealed class ProjectContext
{
    public ProjectInfo Project { get; init; } = new();
    public IReadOnlyList<TaskInfo> Tasks { get; init; } = Array.Empty<TaskInfo>();
    public IReadOnlyList<SecretInfo> Secrets { get; init; } = Array.Empty<SecretInfo>();
    public IDictionary<string, string> Storage { get; init; } = new Dictionary<string, string>();
    // Simple logger callback (optional for plugins)
    public Action<string>? Log { get; init; }
}

public sealed class ProjectInfo
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Owner { get; init; } = string.Empty;
    public string Status { get; init; } = "Active";
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}

public sealed class TaskInfo
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public DateTime? Due { get; init; }
    public bool Completed { get; init; }
    public string? AssignedTo { get; init; }
}

public sealed class SecretInfo
{
    public int Id { get; init; }
    public string Kind { get; init; } = "ApiKey"; // ApiKey, Password, Note
    public string Name { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty; // Decrypted value passed to plugin
    public string? Metadata { get; init; }
}
