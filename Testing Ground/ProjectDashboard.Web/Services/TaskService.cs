using Microsoft.EntityFrameworkCore;
using ProjectDashboard.Web.Data;
using ProjectDashboard.Web.Models;

namespace ProjectDashboard.Web.Services;

public class TaskService
{
    private readonly ApplicationDbContext _db;
    private readonly WebhookService _hooks;

    public TaskService(ApplicationDbContext db, WebhookService hooks)
    {
        _db = db; _hooks = hooks;
    }

    public async Task<List<TaskItem>> GetTasksForProjectAsync(int projectId) =>
        await _db.Tasks.Where(t => t.ProjectId == projectId).OrderBy(t => t.Due).ToListAsync();

    public async Task AddTaskAsync(int projectId, string title, string? assignedTo = null, DateTime? due = null)
    {
        _db.Tasks.Add(new TaskItem { ProjectId = projectId, Title = title, AssignedTo = assignedTo, Due = due });
        await _db.SaveChangesAsync();
    }

    public async Task CompleteAsync(int taskId)
    {
        var t = await _db.Tasks.FirstOrDefaultAsync(x => x.Id == taskId);
        if (t is null) return;
        t.Completed = true;
        await _db.SaveChangesAsync();

        var proj = await _db.Projects.Include(p => p.Webhooks).FirstAsync(p => p.Id == t.ProjectId);
        await _hooks.NotifyTaskCompletedAsync(proj, t, proj.Webhooks);
    }
}
