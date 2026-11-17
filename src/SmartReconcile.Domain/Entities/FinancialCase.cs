namespace SmartReconcile.Domain.Entities;

using SmartReconcile.Domain.Enums;

public sealed class FinancialCase
{
    private FinancialCase() { }

    public Guid Id { get; private set; }
    public Guid? DiscrepancyId { get; private set; }
    public FinancialCaseStatus Status { get; private set; }
    public FinancialCaseSeverity Severity { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? RootCause { get; private set; }
    public decimal AffectedAmount { get; private set; }
    public string Currency { get; private set; } = "EUR";
    public DateTime DetectedAtUtc { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }

    public static FinancialCase Create(
        Guid? discrepancyId,
        FinancialCaseSeverity severity,
        string title,
        string description,
        decimal affectedAmount,
        string? rootCause = null)
        => new()
        {
            Id = Guid.NewGuid(),
            DiscrepancyId = discrepancyId,
            Status = FinancialCaseStatus.Open,
            Severity = severity,
            Title = title.Trim(),
            Description = description.Trim(),
            RootCause = string.IsNullOrWhiteSpace(rootCause) ? null : rootCause.Trim(),
            AffectedAmount = decimal.Round(Math.Abs(affectedAmount), 2),
            Currency = "EUR",
            DetectedAtUtc = DateTime.UtcNow
        };

    public void StartInvestigation() => Status = FinancialCaseStatus.Investigating;

    public void Resolve(string? rootCause = null)
    {
        if (!string.IsNullOrWhiteSpace(rootCause)) RootCause = rootCause.Trim();
        Status = FinancialCaseStatus.Resolved;
        ResolvedAtUtc = DateTime.UtcNow;
    }

    public void Ignore()
    {
        Status = FinancialCaseStatus.Ignored;
        ResolvedAtUtc = DateTime.UtcNow;
    }
}
