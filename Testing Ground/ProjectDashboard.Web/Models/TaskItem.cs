using System.ComponentModel.DataAnnotations;

namespace ProjectDashboard.Web.Models;

public class TaskItem
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = string.Empty;
    [MaxLength(500)] public string? Notes { get; set; }
    public DateTime? Due { get; set; }
    public bool Completed { get; set; }
    [MaxLength(100)] public string? AssignedTo { get; set; }
}
