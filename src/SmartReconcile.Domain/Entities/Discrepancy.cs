namespace SmartReconcile.Domain.Entities;

using SmartReconcile.Domain.Enums;

public sealed class Discrepancy
{
    private Discrepancy() { }

    public Guid Id { get; private set; }
    public DiscrepancyType Type { get; private set; }
    public DiscrepancyStatus Status { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal AffectedAmount { get; private set; }
    public Guid? OrderId { get; private set; }
    public Guid? PaymentId { get; private set; }
    public Guid? InvoiceId { get; private set; }
    public DateTime DetectedAtUtc { get; private set; }
    public string? AiAnalysis { get; private set; }

    public static Discrepancy Create(DiscrepancyType type, string title, string description, decimal affectedAmount, Guid? orderId, Guid? paymentId, Guid? invoiceId, string? aiAnalysis = null)
        => new()
        {
            Id = Guid.NewGuid(), Type = type, Status = DiscrepancyStatus.Open, Title = title, Description = description,
            AffectedAmount = decimal.Round(Math.Abs(affectedAmount), 2), OrderId = orderId, PaymentId = paymentId,
            InvoiceId = invoiceId, DetectedAtUtc = DateTime.UtcNow, AiAnalysis = aiAnalysis
        };

    public void Resolve() => Status = DiscrepancyStatus.Resolved;
    public void Ignore() => Status = DiscrepancyStatus.Ignored;
    public void SetAiAnalysis(string analysis) => AiAnalysis = analysis;
}