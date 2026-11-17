namespace SmartReconcile.Domain.Entities;

using SmartReconcile.Domain.Enums;

public sealed class FinancialCaseLink
{
    private FinancialCaseLink() { }

    public Guid Id { get; private set; }
    public Guid FinancialCaseId { get; private set; }
    public FinancialNodeType NodeType { get; private set; }
    public Guid EntityId { get; private set; }
    public string Role { get; private set; } = string.Empty;
    public string Label { get; private set; } = string.Empty;
    public decimal? Amount { get; private set; }
    public string Currency { get; private set; } = "EUR";

    public static FinancialCaseLink Create(
        Guid financialCaseId,
        FinancialNodeType nodeType,
        Guid entityId,
        string role,
        string label,
        decimal? amount = null)
        => new()
        {
            Id = Guid.NewGuid(),
            FinancialCaseId = financialCaseId,
            NodeType = nodeType,
            EntityId = entityId,
            Role = role.Trim(),
            Label = label.Trim(),
            Amount = amount.HasValue ? decimal.Round(amount.Value, 2) : null,
            Currency = "EUR"
        };
}
