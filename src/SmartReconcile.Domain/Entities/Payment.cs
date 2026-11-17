namespace SmartReconcile.Domain.Entities;

using SmartReconcile.Domain.Enums;

public sealed class Payment
{
    private Payment() { }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public string TransactionId { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public decimal FeeAmount { get; private set; }
    public string Currency { get; private set; } = "EUR";
    public PaymentStatus Status { get; private set; }
    public DateTime PaidAtUtc { get; private set; }
    public Order? Order { get; private set; }

    public static Payment Create(
        Guid orderId,
        string transactionId,
        decimal amount,
        PaymentStatus status,
        DateTime paidAtUtc,
        decimal feeAmount = 0m)
    {
        if (orderId == Guid.Empty) throw new ArgumentException("Order id is required.", nameof(orderId));
        if (string.IsNullOrWhiteSpace(transactionId)) throw new ArgumentException("Transaction id is required.", nameof(transactionId));
        if (feeAmount < 0) throw new ArgumentOutOfRangeException(nameof(feeAmount));

        return new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            TransactionId = transactionId.Trim(),
            Amount = decimal.Round(amount, 2),
            FeeAmount = decimal.Round(feeAmount, 2),
            Currency = "EUR",
            Status = status,
            PaidAtUtc = paidAtUtc
        };
    }
}
