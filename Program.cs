using FiresideEditorial.Components;
using FiresideEditorial.Data;
using FiresideEditorial.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=fireside.db"));

builder.Services.AddScoped<IContentService, EfContentService>();
builder.Services.AddScoped<SearchState>();
builder.Services.AddScoped<INewsletterService, ButtondownNewsletterService>();
builder.Services.AddHttpClient();

var app = builder.Build();

// Auto-migrate and seed database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
    await db.Database.EnsureCreatedAsync();
    await DbSeeder.SeedAsync(db, env);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
