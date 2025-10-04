using API.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Create db in azure app service read/write persistent directory or in the local root folder
var home = Environment.GetEnvironmentVariable("HOME") ?? Directory.GetCurrentDirectory();
var dbPath = Path.Combine(home, builder.Configuration.GetValue<string>("DatabaseName")!);
builder.Services
    .AddDbContext<AppDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Ensure database is created and latest migrations are applied
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

if (builder.Configuration.GetValue<bool>("EnableSwaggerUI"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();
