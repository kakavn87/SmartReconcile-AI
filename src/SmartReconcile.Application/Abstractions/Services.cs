using SmartReconcile.Application.DTOs;

namespace SmartReconcile.Application.Abstractions;

public interface IOrderService
{
    Task<IReadOnlyList<OrderDto>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<OrderDto?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);
    Task<OrderDto> CreateAsync(Guid tenantId, CreateOrderRequest request, CancellationToken cancellationToken);
}

public interface IPaymentService
{
    Task<IReadOnlyList<PaymentDto>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<PaymentDto?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);
    Task<PaymentDto> CreateAsync(Guid tenantId, CreatePaymentRequest request, CancellationToken cancellationToken);
}

public interface IInvoiceService
{
    Task<IReadOnlyList<InvoiceDto>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<InvoiceDto?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);
    Task<InvoiceDto> CreateAsync(Guid tenantId, CreateInvoiceRequest request, CancellationToken cancellationToken);
}

public interface IDiscrepancyService
{
    Task<IReadOnlyList<DiscrepancyDto>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<DiscrepancyDto?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken cancellationToken);
    Task<DiscrepancyDto?> UpdateStatusAsync(Guid tenantId, Guid id, UpdateDiscrepancyStatusRequest request, CancellationToken cancellationToken);
}

public interface IReconciliationService
{
    Task<ReconciliationResult> ReconcileAsync(Guid tenantId, CancellationToken cancellationToken);
}

public interface IDashboardService
{
    Task<DashboardDto> GetAsync(Guid tenantId, CancellationToken cancellationToken);
}

public interface IAiAnalysisService
{
    Task<AiAnalysisResponse> AnalyzeInvoiceAsync(AiAnalysisRequest request, CancellationToken cancellationToken);
    Task<FinancialCaseAnalysisResponse> AnalyzeFinancialCaseAsync(FinancialCaseAnalysisRequest request, CancellationToken cancellationToken);
}

public interface ICsvExporter
{
    Task<byte[]> ExportDiscrepanciesAsync(Guid tenantId, CancellationToken cancellationToken);
}
