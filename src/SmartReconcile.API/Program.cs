using Microsoft.EntityFrameworkCore;
using SmartReconcile.Application;
using SmartReconcile.Infrastructure;
using SmartReconcile.Infrastructure.Persistence;
using SmartReconcile.Infrastructure.Services;
using SmartReconcile.API.Integrations;
using SmartReconcile.Application.Abstractions;

var builder = WebApplication.CreateBuilder(args);
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<ShopifyOptions>(builder.Configuration.GetSection("Shopify"));
builder.Services.AddHttpClient();
builder.Services.AddSingleton<ShopifyOAuthStateStore>();
builder.Services.AddScoped<IShopifyOAuthService, ShopifyOAuthService>();
builder.Services.AddScoped<IShopifyWebhookService, ShopifyWebhookService>();
builder.Services.AddScoped<IShopifyClient, ShopifyClient>();
builder.Services.AddScoped<IShopifySyncService, ShopifySyncService>();

var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:4200"];
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy => policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
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

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("Frontend");
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "SmartReconcile.API", utc = DateTime.UtcNow }));
app.MapControllers();

await app.RunAsync();

public partial class Program { }
