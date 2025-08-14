using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProjectDashboard.Web.Models;

namespace ProjectDashboard.Web.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<SensitiveItem> SensitiveItems => Set<SensitiveItem>();
    public DbSet<Webhook> Webhooks => Set<Webhook>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Project>()
            .HasMany(p => p.Tasks)
            .WithOne()
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Project>()
            .HasMany(p => p.Webhooks)
            .WithOne()
            .HasForeignKey(w => w.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
