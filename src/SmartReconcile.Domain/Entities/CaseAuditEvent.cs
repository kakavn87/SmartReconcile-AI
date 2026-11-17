namespace SmartReconcile.Domain.Entities;

using SmartReconcile.Domain.Enums;

public sealed class CaseAuditEvent
{
    private CaseAuditEvent() { }

    public Guid Id { get; private set; }
    public Guid FinancialCaseId { get; private set; }
    public CaseAuditEventType EventType { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string Actor { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    public static CaseAuditEvent Create(
        Guid financialCaseId,
        CaseAuditEventType eventType,
        string description,
        string actor)
        => new()
        {
            Id = Guid.NewGuid(),
            FinancialCaseId = financialCaseId,
            EventType = eventType,
            Description = description.Trim(),
            Actor = actor.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };
}
