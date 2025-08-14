using System.ComponentModel.DataAnnotations;

namespace ProjectDashboard.Web.Models;

public class Webhook
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    [Required, MaxLength(500)] public string Url { get; set; } = string.Empty;
    [MaxLength(200)] public string? Secret { get; set; }
    [MaxLength(100)] public string Event { get; set; } = "TaskCompleted";
    public bool Enabled { get; set; } = true;
}
