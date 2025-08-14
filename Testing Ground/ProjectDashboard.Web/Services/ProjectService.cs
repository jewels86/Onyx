using Microsoft.EntityFrameworkCore;
using ProjectDashboard.Web.Data;
using ProjectDashboard.Web.Models;

namespace ProjectDashboard.Web.Services;

public class ProjectService
{
    private readonly ApplicationDbContext _db;
    public ProjectService(ApplicationDbContext db) { _db = db; }

    public async Task<List<Project>> GetProjectsAsync(string? ownerUserId = null)
    {
        var q = _db.Projects.Include(p => p.Tasks).Include(p => p.Webhooks).AsQueryable();
        if (!string.IsNullOrEmpty(ownerUserId)) q = q.Where(p => p.OwnerUserId == ownerUserId);
        return await q.OrderByDescending(p => p.CreatedAt).ToListAsync();
    }

    public async Task<Project> CreateAsync(string name, string ownerUserId, string? description = null)
    {
        var p = new Project { Name = name, OwnerUserId = ownerUserId, Description = description };
        _db.Projects.Add(p);
        await _db.SaveChangesAsync();
        return p;
    }
}
