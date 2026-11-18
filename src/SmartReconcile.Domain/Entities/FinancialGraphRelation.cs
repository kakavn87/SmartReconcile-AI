namespace SmartReconcile.Domain.Entities;

using SmartReconcile.Domain.Enums;

public sealed class FinancialGraphRelation
{
    private FinancialGraphRelation() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public FinancialNodeType FromNodeType { get; private set; }
    public Guid FromEntityId { get; private set; }
    public FinancialNodeType ToNodeType { get; private set; }
    public Guid ToEntityId { get; private set; }
    public FinancialRelationType RelationType { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static FinancialGraphRelation Create(Guid tenantId, FinancialNodeType fromNodeType, Guid fromEntityId, FinancialNodeType toNodeType, Guid toEntityId, FinancialRelationType relationType)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        return new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FromNodeType = fromNodeType,
            FromEntityId = fromEntityId,
            ToNodeType = toNodeType,
            ToEntityId = toEntityId,
            RelationType = relationType,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}
