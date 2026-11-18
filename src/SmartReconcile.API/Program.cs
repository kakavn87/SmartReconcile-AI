using Microsoft.EntityFrameworkCore;
using SmartReconcile.Application;
using SmartReconcile.Infrastructure;
using SmartReconcile.Infrastructure.Persistence;
using SmartReconcile.API.Integrations;

var builder = WebApplication.CreateBuilder(args);
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<ShopifyOptions>(builder.Configuration.GetSection("Shopify"));

var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:4200"];
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy => policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("Frontend");
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "SmartReconcile.API", utc = DateTime.UtcNow }));
app.MapControllers();

// Start listening before database initialization so Render can detect the port.
// Database/schema initialization is intentionally deferred until the HTTP server is up.
var runTask = app.RunAsync();

try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<SmartReconcileDbContext>();

    // The demo currently has no EF migration files.
    // A previous startup attempt may have created only EF's migration-history table.
    // If the application tables are missing, remove that stale table so EnsureCreated
    // can create the complete schema.
    var ordersTableExists = await db.Database
        .SqlQueryRaw<bool>(@"SELECT to_regclass('public.""Orders""') IS NOT NULL AS ""Value""")
        .SingleAsync();

    if (!ordersTableExists)
    {
        await db.Database.ExecuteSqlRawAsync(@"DROP TABLE IF EXISTS ""__EFMigrationsHistory"";");
    }

    await db.Database.EnsureCreatedAsync();
    await IntegrationSchemaInitializer.EnsureCreatedAsync(db);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Database initialization failed: {ex}");
    await app.StopAsync();
    throw;
}

await runTask;

public partial class Program { }
