using System.Net.Http.Json;
using ProjectDashboard.Web.Models;

namespace ProjectDashboard.Web.Services;

public class WebhookService
{
    private readonly HttpClient _http = new();

    public async Task NotifyTaskCompletedAsync(Project project, TaskItem task, IEnumerable<Webhook> hooks)
    {
        var payload = new
        {
            type = "TaskCompleted",
            project = project.Name,
            task = task.Title,
            whenUtc = DateTime.UtcNow
        };
        foreach (var h in hooks.Where(h => h.Enabled && h.Event == "TaskCompleted"))
        {
            try
            {
                await _http.PostAsJsonAsync(h.Url, payload);
            }
            catch
            {
                // swallow for demo
            }
        }
    }
}
