using SmartReconcile.Domain.Entities;
using SmartReconcile.Domain.Enums;
using Xunit;

namespace SmartReconcile.Tests;

public sealed class ReconciliationTests
{
    [Fact]
    public void Order_Create_RoundsAmountToTwoDecimals()
    {
        var order = Order.Create("DE-1", "Max Mustermann", "max@example.de", 123.456m, OrderStatus.Pending, DateTime.UtcNow);
        Assert.Equal(123.46m, order.TotalAmount);
    }

    [Fact]
    public void Discrepancy_DefaultsToOpen()
    {
        var item = Discrepancy.Create(DiscrepancyType.MissingPayment, "Zahlung fehlt", "Test", 10m, Guid.NewGuid(), null, null);
        Assert.Equal(DiscrepancyStatus.Open, item.Status);
        item.Resolve();
        Assert.Equal(DiscrepancyStatus.Resolved, item.Status);
    }
}