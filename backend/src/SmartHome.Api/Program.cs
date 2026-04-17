using Microsoft.EntityFrameworkCore;
using SmartHome.Domain.Device.Repository;
using SmartHome.Infrastructure.Device.Repository;
using SmartHome.Infrastructure.Persistence;
using SmartHome.Infrastructure.Persistence.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var dataDirectory = Path.GetFullPath(
    Path.Combine(builder.Environment.ContentRootPath, "..", "..", "..", "data"));

Directory.CreateDirectory(dataDirectory);

var databasePath = Path.Combine(dataDirectory, "smarthome.db");

builder.Services.AddDbContext<SmartHomeDbContext>(options =>
    options.UseSqlite($"Data Source={databasePath}"));

builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
builder.Services.AddScoped<SmartHomeDbSeeder>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Logger.LogDebug("DB Path: {DatabasePath}", databasePath);

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SmartHomeDbContext>();
    await dbContext.Database.MigrateAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<SmartHomeDbSeeder>();
    await seeder.SeedAsync();
}

app.Run();