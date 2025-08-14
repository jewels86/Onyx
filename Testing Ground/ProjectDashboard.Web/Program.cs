using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProjectDashboard.Web.Data;
using ProjectDashboard.Web.Services;
using ProjectDashboard.Web.Services.Plugins;

var builder = WebApplication.CreateBuilder(args);
// Db + Identity
builder.Services.AddDbContext<ApplicationDbContext>(opts =>
    opts.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
//builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultUI();

// Blazor
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// App services
builder.Services.AddSingleton<EncryptionService>();
builder.Services.AddScoped<EmailSender>();
builder.Services.AddScoped<WebhookService>();
builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<SensitiveDataService>();
builder.Services.AddScoped<ProjectContextBuilder>();

// Plugin host (loads on startup)
builder.Services.AddSingleton<PluginHost>();

var app = builder.Build();

// Ensure database & plugins folder
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();
    var host = scope.ServiceProvider.GetRequiredService<PluginHost>();
    var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
    var pluginPath = Path.Combine(env.ContentRootPath, "Plugins");
    Directory.CreateDirectory(pluginPath);
    host.LoadFromFolder(pluginPath);
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    //app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages(); // Identity UI
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
