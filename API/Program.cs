using API;
using API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Create db in azure app service read/write persistent directory or in the local root folder
var home = Environment.GetEnvironmentVariable("HOME") ?? Directory.GetCurrentDirectory();
var dbPath = Path.Combine(home, builder.Configuration.GetValue<string>("DatabaseName")!);
builder.Services
    .AddDbContext<AppDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddAntiforgery();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

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

app.UseAntiforgery();

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/" ||
        context.Request.Path.StartsWithSegments("/swagger") ||
        context.Request.Path.StartsWithSegments("/auth"))
    {
        await next.Invoke();
        return;
    }

    var tokenValue = builder.Configuration.GetValue<string>("bearerToken");
    if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader) ||
        string.IsNullOrWhiteSpace(authHeader) ||
        !authHeader.ToString().StartsWith("Bearer ") ||
        authHeader.ToString() != $"Bearer {tokenValue}")
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Unauthorized");
        return;
    }
    await next.Invoke();
});

app.MapGet("/auth/getToken", () =>
{
    return Results.Ok(new
    {
        Token = builder.Configuration.GetValue<string>("bearerToken")
    });
});

app.RegisterMyEndpoints();

app.UseHttpsRedirection();

app.Run();
