using Microsoft.Extensions.DependencyInjection;
using SmartReconcile.Application.Abstractions;
using SmartReconcile.Application.Services;

namespace SmartReconcile.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IDiscrepancyService, DiscrepancyService>();
        services.AddScoped<IReconciliationService, ReconciliationService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IIntegrationService, IntegrationService>();
        services.AddScoped<IControlTowerService, ControlTowerService>();
        services.AddScoped<IAccountingSettingsService, AccountingSettingsService>();
        return services;
    }
}