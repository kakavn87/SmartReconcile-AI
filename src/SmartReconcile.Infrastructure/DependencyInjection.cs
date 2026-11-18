using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Infrastructure.Persistence;
using SmartReconcile.Infrastructure.Repositories;
using SmartReconcile.Infrastructure.Services;
using SmartReconcile.Infrastructure.Workers;

namespace SmartReconcile.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = ConnectionStringParser.Normalize(configuration.GetConnectionString("DefaultConnection"));
        services.AddDbContext<SmartReconcileDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IDiscrepancyRepository, DiscrepancyRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IIntegrationConnectionRepository, IntegrationConnectionRepository>();
        services.AddSingleton<ISecretProtector, AesGcmSecretProtector>();
        services.AddScoped<IWebhookEventRepository, WebhookEventRepository>();
        services.AddScoped<IFinancialCaseRepository, FinancialCaseRepository>();
        services.AddScoped<IFinancialCaseLinkRepository, FinancialCaseLinkRepository>();
        services.AddScoped<ICaseAuditEventRepository, CaseAuditEventRepository>();
        services.AddScoped<IFinancialGraphRepository, FinancialGraphRepository>();
        services.AddScoped<IAccountingSettingsRepository, AccountingSettingsRepository>();
        services.AddScoped<IDatevExtfExporter, DatevExtfExporter>();
        services.AddScoped<ICsvExporter, CsvExporter>();
        services.AddHttpClient<IAiAnalysisService, AiAnalysisService>();
        services.AddHostedService<ShopifyWebhookWorker>();
        services.AddHostedService<ReconciliationWorker>();
        return services;
    }
}

internal static class ConnectionStringParser
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");
        if (!value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) && !value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)) return value;
        var uri = new Uri(value);
        var userInfo = uri.UserInfo.Split(':', 2);
        var builder = new Npgsql.NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = uri.AbsolutePath.Trim('/'),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
            SslMode = Npgsql.SslMode.Require
        };
        return builder.ConnectionString;
    }
}
