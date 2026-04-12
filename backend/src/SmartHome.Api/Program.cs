using Microsoft.EntityFrameworkCore;
using SmartHome.Infrastructure.Persistence;
using SmartHome.Infrastructure.Persistence.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// Build absolute path to repo-level /data folder
var dataDirectory = Path.GetFullPath(
    Path.Combine(builder.Environment.ContentRootPath, "..", "..", "..", "data"));

Directory.CreateDirectory(dataDirectory);

var databasePath = Path.Combine(dataDirectory, "smarthome.db");

// Register DbContext
builder.Services.AddDbContext<SmartHomeDbContext>(options =>
    options.UseSqlite($"Data Source={databasePath}"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Debug output to confirm path
Console.WriteLine($"DB Path: {databasePath}");

// Apply migrations + seed data
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SmartHomeDbContext>();

    // Apply migrations automatically
    await dbContext.Database.MigrateAsync();

    // Seed initial data (idempotent)
    var seeder = new SmartHomeDbSeeder(dbContext);
    await seeder.SeedAsync();
}

app.Run();