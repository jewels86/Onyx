using System.ComponentModel.DataAnnotations;

namespace ProjectDashboard.Web.Models;

public class Project
{
    public int Id { get; set; }
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    [MaxLength(1000)] public string? Description { get; set; }
    [MaxLength(50)] public string Status { get; set; } = "Active"; // Active, OnHold, Done
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string OwnerUserId { get; set; } = string.Empty;

    public List<TaskItem> Tasks { get; set; } = new();
    public List<Webhook> Webhooks { get; set; } = new();
}
