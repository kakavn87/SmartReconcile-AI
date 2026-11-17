using Microsoft.EntityFrameworkCore;
using SmartReconcile.Application;
using SmartReconcile.Infrastructure;
using SmartReconcile.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:4200"];
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy => policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SmartReconcileDbContext>();

    // The demo currently has no EF migration files.
    // EnsureCreated creates the complete PostgreSQL schema on a fresh Neon database.
    await db.Database.EnsureCreatedAsync();
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("Frontend");
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "SmartReconcile.API", utc = DateTime.UtcNow }));
app.MapControllers();

await app.RunAsync();

public partial class Program { }
