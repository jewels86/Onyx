using System.ComponentModel.DataAnnotations;

namespace ProjectDashboard.Web.Models;

public class SensitiveItem
{
    public int Id { get; set; }
    public int? ProjectId { get; set; }
    [Required, MaxLength(100)] public string Kind { get; set; } = "ApiKey"; // ApiKey, Password, Note
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    [Required] public string EncryptedValueBase64 { get; set; } = string.Empty;
    [Required] public string IvBase64 { get; set; } = string.Empty;
    [MaxLength(200)] public string? Metadata { get; set; }
    public string OwnerUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
