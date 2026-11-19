namespace SmartReconcile.Domain.Entities;

using SmartReconcile.Domain.Enums;

public sealed class Payment
{
    private Payment() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid OrderId { get; private set; }
    public string TransactionId { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public decimal FeeAmount { get; private set; }
    public PaymentKind Kind { get; private set; }
    public string? Gateway { get; private set; }
    public string Currency { get; private set; } = "EUR";
    public PaymentStatus Status { get; private set; }
    public DateTime PaidAtUtc { get; private set; }
    public Order? Order { get; private set; }

    public static Payment Create(
        Guid tenantId,
        Guid orderId,
        string transactionId,
        decimal amount,
        PaymentStatus status,
        DateTime paidAtUtc,
        decimal feeAmount = 0m,
        string currency = "EUR",
        PaymentKind kind = PaymentKind.Sale,
        string? gateway = null)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        if (orderId == Guid.Empty) throw new ArgumentException("Order id is required.", nameof(orderId));
        if (string.IsNullOrWhiteSpace(transactionId)) throw new ArgumentException("Transaction id is required.", nameof(transactionId));
        if (feeAmount < 0) throw new ArgumentOutOfRangeException(nameof(feeAmount));

        return new Payment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OrderId = orderId,
            TransactionId = transactionId.Trim(),
            Amount = decimal.Round(amount, 2),
            FeeAmount = decimal.Round(feeAmount, 2),
            Currency = string.IsNullOrWhiteSpace(currency) ? "EUR" : currency.Trim().ToUpperInvariant(),
            Kind = kind,
            Gateway = string.IsNullOrWhiteSpace(gateway) ? null : gateway.Trim(),
            Status = status,
            PaidAtUtc = paidAtUtc
        };
    }

    public void UpdateFromShopify(decimal amount, string currency, PaymentStatus status, DateTime paidAtUtc, PaymentKind kind, string? gateway, decimal feeAmount = 0m)
    {
        Amount = decimal.Round(Math.Max(amount, 0m), 2);
        Currency = string.IsNullOrWhiteSpace(currency) ? Currency : currency.Trim().ToUpperInvariant();
        Status = status;
        PaidAtUtc = paidAtUtc;
        Kind = kind;
        Gateway = string.IsNullOrWhiteSpace(gateway) ? Gateway : gateway.Trim();
        FeeAmount = decimal.Round(Math.Max(feeAmount, 0m), 2);
    }

    public void ChangeStatus(PaymentStatus status) => Status = status;
}
