namespace SmartReconcile.Domain.Entities;

public sealed class Tenant
{
    private Tenant() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static Tenant Create(string name, string slug)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tenant name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(slug)) throw new ArgumentException("Tenant slug is required.", nameof(slug));

        return new Tenant
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}
