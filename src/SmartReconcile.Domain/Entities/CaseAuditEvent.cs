namespace SmartReconcile.Domain.Entities;

using SmartReconcile.Domain.Enums;

public sealed class CaseAuditEvent
{
    private CaseAuditEvent() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid FinancialCaseId { get; private set; }
    public CaseAuditEventType EventType { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string Actor { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    public static CaseAuditEvent Create(Guid tenantId, Guid financialCaseId, CaseAuditEventType eventType, string description, string actor)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        return new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FinancialCaseId = financialCaseId,
            EventType = eventType,
            Description = description.Trim(),
            Actor = actor.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };
    }
    public static CaseAuditEvent Create(Guid tenantId, Guid financialCaseId, CaseAuditEventType eventType, string description, string actor)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        return new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FinancialCaseId = financialCaseId,
            EventType = eventType,
            Description = description.Trim(),
            Actor = actor.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public bool NormalizeLegacyEventType()
    {
        if ((int)EventType != 0)
            return false;

        EventType = CaseAuditEventType.Detected;
        return true;
    }
}
