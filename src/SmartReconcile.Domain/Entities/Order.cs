namespace SmartReconcile.Domain.Entities;

using SmartReconcile.Domain.Enums;

public sealed class Order
{
    private Order() { }

    public Guid Id { get; private set; }
    public string OrderNumber { get; private set; } = string.Empty;
    public string CustomerName { get; private set; } = string.Empty;
    public string CustomerEmail { get; private set; } = string.Empty;
    public decimal TotalAmount { get; private set; }
    public string Currency { get; private set; } = "EUR";
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Payment? Payment { get; private set; }

    public static Order Create(string orderNumber, string customerName, string customerEmail, decimal totalAmount, OrderStatus status, DateTime createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(orderNumber)) throw new ArgumentException("Order number is required.", nameof(orderNumber));
        if (totalAmount < 0) throw new ArgumentOutOfRangeException(nameof(totalAmount));
        return new Order
        {
            Id = Guid.NewGuid(), OrderNumber = orderNumber.Trim(), CustomerName = customerName.Trim(),
            CustomerEmail = customerEmail.Trim(), TotalAmount = decimal.Round(totalAmount, 2), Currency = "EUR",
            Status = status, CreatedAtUtc = createdAtUtc
        };
    }

    public void ChangeStatus(OrderStatus status) => Status = status;
}