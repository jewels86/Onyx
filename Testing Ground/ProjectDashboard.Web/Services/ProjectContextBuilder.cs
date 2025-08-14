using Microsoft.EntityFrameworkCore;
using ProjectDashboard.Abstractions;
using ProjectDashboard.Web.Data;

namespace ProjectDashboard.Web.Services;

public class ProjectContextBuilder
{
    private readonly ApplicationDbContext _db;
    private readonly EncryptionService _enc;

    public ProjectContextBuilder(ApplicationDbContext db, EncryptionService enc)
    {
        _db = db; _enc = enc;
    }

    public async Task<ProjectContext?> BuildAsync(int projectId, string requesterUserName)
    {
        var project = await _db.Projects.Include(p => p.Tasks).FirstOrDefaultAsync(p => p.Id == projectId);
        if (project is null) return null;

        // For demo, we allow requester to see project regardless of ownership
        var secrets = await _db.SensitiveItems.Where(s => s.ProjectId == projectId).ToListAsync();

        var ctx = new ProjectContext
        {
            Project = new ProjectInfo
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                Owner = project.OwnerUserId,
                Status = project.Status,
                CreatedAtUtc = project.CreatedAt
            },
            Tasks = project.Tasks.Select(t => new TaskInfo
            {
                Id = t.Id,
                Title = t.Title,
                Notes = t.Notes,
                Due = t.Due,
                Completed = t.Completed,
                AssignedTo = t.AssignedTo
            }).ToList(),
            Secrets = secrets.Select(s => new SecretInfo
            {
                Id = s.Id,
                Kind = s.Kind,
                Name = s.Name,
                Value = _enc.Decrypt(s.EncryptedValueBase64, s.IvBase64),
                Metadata = s.Metadata
            }).ToList(),
            Storage = new Dictionary<string, string>() // placeholder for future use
        };

        return ctx;
    }
}
