namespace SmartReconcile.Domain.Entities;

public sealed class WebhookEvent
{
    private WebhookEvent() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid ConnectionId { get; private set; }
    public string ExternalEventId { get; private set; } = string.Empty;
    public string EventType { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = string.Empty;
    public DateTime ReceivedAtUtc { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }
    public bool IsProcessed { get; private set; }

    public static WebhookEvent Create(
        Guid tenantId,
        Guid connectionId,
        string externalEventId,
        string eventType,
        string payloadJson)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant id is required.", nameof(tenantId));
        if (connectionId == Guid.Empty) throw new ArgumentException("Connection id is required.", nameof(connectionId));
        if (string.IsNullOrWhiteSpace(externalEventId)) throw new ArgumentException("External event id is required.", nameof(externalEventId));
        if (string.IsNullOrWhiteSpace(eventType)) throw new ArgumentException("Event type is required.", nameof(eventType));

        return new WebhookEvent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ConnectionId = connectionId,
            ExternalEventId = externalEventId.Trim(),
            EventType = eventType.Trim(),
            PayloadJson = payloadJson ?? "{}",
            ReceivedAtUtc = DateTime.UtcNow,
            IsProcessed = false
        };
    }

    public void MarkProcessed() 
    {
        IsProcessed = true;
        ProcessedAtUtc = DateTime.UtcNow;
    }
}
