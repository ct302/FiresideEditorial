using FiresideEditorial.Components;
using FiresideEditorial.Data;
using FiresideEditorial.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=fireside.db"));

// NOTE: Using JsonContentService instead of EfContentService — Azure F1 free tier
// has issues loading SQLite native libs. Swap back to EfContentService when on B1+.
builder.Services.AddScoped<IContentService, JsonContentService>();
builder.Services.AddScoped<SearchState>();
builder.Services.AddScoped<INewsletterService, ButtondownNewsletterService>();
builder.Services.AddSingleton<IGiftGuideService, JsonGiftGuideService>();
builder.Services.AddSingleton<IRecipeService, JsonRecipeService>();
builder.Services.AddSingleton<IShopService, JsonShopService>();
builder.Services.AddSingleton<AdminAuthService>();
builder.Services.AddHttpClient();

// Cookie authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/admin/login";
        options.LogoutPath = "/admin/logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

// Auto-migrate and seed database (wrapped — F1 tier may fail SQLite native init)
try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        await db.Database.EnsureCreatedAsync();
        await DbSeeder.SeedAsync(db, env);
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Database initialization failed — continuing without DB seed.");
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Login/logout endpoints (minimal API — cookie auth needs HTTP context)
app.MapPost("/admin/login-handler", async (HttpContext ctx, AdminAuthService auth) =>
{
    var form = await ctx.Request.ReadFormAsync();
    var username = form["username"].ToString();
    var password = form["password"].ToString();

    if (auth.ValidateCredentials(username, password))
    {
        var principal = auth.CreatePrincipal(username);
        await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        ctx.Response.Redirect("/admin");
    }
    else
    {
        ctx.Response.Redirect("/admin/login?error=1");
    }
});

app.MapGet("/admin/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    ctx.Response.Redirect("/");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
