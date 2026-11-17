namespace SmartReconcile.Domain.Entities;

using SmartReconcile.Domain.Enums;

public sealed class FinancialGraphRelation
{
    private FinancialGraphRelation() { }

    public Guid Id { get; private set; }
    public FinancialNodeType FromNodeType { get; private set; }
    public Guid FromEntityId { get; private set; }
    public FinancialNodeType ToNodeType { get; private set; }
    public Guid ToEntityId { get; private set; }
    public FinancialRelationType RelationType { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static FinancialGraphRelation Create(
        FinancialNodeType fromNodeType,
        Guid fromEntityId,
        FinancialNodeType toNodeType,
        Guid toEntityId,
        FinancialRelationType relationType)
        => new()
        {
            Id = Guid.NewGuid(),
            FromNodeType = fromNodeType,
            FromEntityId = fromEntityId,
            ToNodeType = toNodeType,
            ToEntityId = toEntityId,
            RelationType = relationType,
            CreatedAtUtc = DateTime.UtcNow
        };
}
