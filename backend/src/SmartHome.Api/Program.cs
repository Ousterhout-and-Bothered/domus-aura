using Microsoft.EntityFrameworkCore;
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();


Console.WriteLine($"DB Path: {databasePath}");


using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SmartHomeDbContext>();

    
    await dbContext.Database.MigrateAsync();

    
    var seeder = new SmartHomeDbSeeder(dbContext);
    await seeder.SeedAsync();
}

app.Run();