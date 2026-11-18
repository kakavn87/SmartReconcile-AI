using SmartReconcile.Application.DTOs;

namespace SmartReconcile.Application.Abstractions;

public interface IOrderService
{
    Task<IReadOnlyList<OrderDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<OrderDto>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<OrderDto?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken);
    Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken);
}

public interface IPaymentService
{
    Task<IReadOnlyList<PaymentDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<PaymentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PaymentDto> CreateAsync(CreatePaymentRequest request, CancellationToken cancellationToken);
}

public interface IInvoiceService
{
    Task<IReadOnlyList<InvoiceDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<InvoiceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<InvoiceDto> CreateAsync(CreateInvoiceRequest request, CancellationToken cancellationToken);
}

public interface IDiscrepancyService
{
    Task<IReadOnlyList<DiscrepancyDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<DiscrepancyDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<DiscrepancyDto?> UpdateStatusAsync(Guid id, UpdateDiscrepancyStatusRequest request, CancellationToken cancellationToken);
}

public interface IReconciliationService
{
    Task<ReconciliationResult> ReconcileAsync(CancellationToken cancellationToken);
}

public interface IDashboardService
{
    Task<DashboardDto> GetAsync(CancellationToken cancellationToken);
}

public interface IAiAnalysisService
{
    Task<AiAnalysisResponse> AnalyzeInvoiceAsync(AiAnalysisRequest request, CancellationToken cancellationToken);
    Task<FinancialCaseAnalysisResponse> AnalyzeFinancialCaseAsync(FinancialCaseAnalysisRequest request, CancellationToken cancellationToken);
}

public interface ICsvExporter
{
    Task<byte[]> ExportDiscrepanciesAsync(CancellationToken cancellationToken);
}
