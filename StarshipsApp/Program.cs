using Microsoft.EntityFrameworkCore;
using StarshipsApp.Data;

var builder = WebApplication.CreateBuilder(args);

// Resolve connection string with Azure fallback
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("CUSTOMCONNSTR_DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("DefaultConnection is not configured.");

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

var app = builder.Build();

// Ensure SQLite directory exists on Linux App Service (e.g., /home/data)
if (app.Environment.IsProduction() && OperatingSystem.IsLinux())
{
    const string key = "Data Source=";
    var idx = connectionString.IndexOf(key, StringComparison.OrdinalIgnoreCase);
    if (idx >= 0)
    {
        var value = connectionString[(idx + key.Length)..];
        var end = value.IndexOf(';');
        var filePath = (end >= 0 ? value[..end] : value).Trim().Trim('"');
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
            app.Logger.LogInformation("Ensured SQLite data directory exists at {Dir}", dir);
        }
    }
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Starships}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DbInitializer.SeedAsync(db, logger);
}

app.Run();