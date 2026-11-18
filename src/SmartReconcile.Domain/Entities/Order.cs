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
    public decimal TaxRate { get; private set; } = 19m;
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Payment? Payment { get; private set; }

    public static Order Create(
        string orderNumber,
        string customerName,
        string customerEmail,
        decimal totalAmount,
        OrderStatus status,
        DateTime createdAtUtc,
        decimal taxRate = 19m,
        string currency = "EUR")
    {
        if (string.IsNullOrWhiteSpace(orderNumber)) throw new ArgumentException("Order number is required.", nameof(orderNumber));
        if (totalAmount < 0) throw new ArgumentOutOfRangeException(nameof(totalAmount));
        if (taxRate is not (7m or 19m)) throw new ArgumentOutOfRangeException(nameof(taxRate), "Nur 7 % oder 19 % Umsatzsteuer werden unterstützt.");

        return new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = orderNumber.Trim(),
            CustomerName = customerName.Trim(),
            CustomerEmail = customerEmail.Trim(),
            TotalAmount = decimal.Round(totalAmount, 2),
            Currency = string.IsNullOrWhiteSpace(currency) ? "EUR" : currency.Trim().ToUpperInvariant(),
            TaxRate = taxRate,
            Status = status,
            CreatedAtUtc = createdAtUtc
        };
    }

    public void ChangeStatus(OrderStatus status) => Status = status;

    public void UpdateFromShopify(
        decimal totalAmount,
        string currency,
        OrderStatus status,
        DateTime createdAtUtc)
    {
        TotalAmount = decimal.Round(Math.Max(totalAmount, 0m), 2);
        Currency = string.IsNullOrWhiteSpace(currency) ? Currency : currency.Trim().ToUpperInvariant();
        Status = status;
        CreatedAtUtc = createdAtUtc;
    }

    public void RedactCustomerData()
    {
        CustomerName = "Shopify Kunde";
        CustomerEmail = string.Empty;
    }
}
