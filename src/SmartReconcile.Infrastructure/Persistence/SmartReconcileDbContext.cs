using Microsoft.EntityFrameworkCore;
using SmartReconcile.Domain.Entities;

namespace SmartReconcile.Infrastructure.Persistence;

public sealed class SmartReconcileDbContext(DbContextOptions<SmartReconcileDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Discrepancy> Discrepancies => Set<Discrepancy>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<IntegrationConnection> IntegrationConnections => Set<IntegrationConnection>();
    public DbSet<WebhookEvent> WebhookEvents => Set<WebhookEvent>();
    public DbSet<FinancialCase> FinancialCases => Set<FinancialCase>();
    public DbSet<FinancialCaseLink> FinancialCaseLinks => Set<FinancialCaseLink>();
    public DbSet<FinancialGraphRelation> FinancialGraphRelations => Set<FinancialGraphRelation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.OrderNumber).IsUnique();
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.Property(x => x.OrderNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.CustomerName).HasMaxLength(200).IsRequired();
            e.Property(x => x.CustomerEmail).HasMaxLength(320).IsRequired();
            e.HasOne(x => x.Payment).WithOne(x => x.Order).HasForeignKey<Payment>(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Payment>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.TransactionId).IsUnique();
            e.Property(x => x.Amount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Invoice>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.InvoiceNumber);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.RawText).HasColumnType("text");
        });

        modelBuilder.Entity<Discrepancy>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Type, x.Status, x.OrderId, x.PaymentId, x.InvoiceId });
            e.Property(x => x.AffectedAmount).HasPrecision(18, 2);
            e.Property(x => x.Description).HasColumnType("text");
            e.Property(x => x.AiAnalysis).HasColumnType("text");
        });

        modelBuilder.Entity<Tenant>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<IntegrationConnection>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ApiKeyHash).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.Provider, x.ExternalStoreId }).IsUnique();
            e.Property(x => x.ExternalStoreId).HasMaxLength(200).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            e.Property(x => x.ApiKeyHash).HasMaxLength(64).IsRequired();
            e.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FinancialCase>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.DiscrepancyId, x.Status });
            e.Property(x => x.Title).HasMaxLength(300).IsRequired();
            e.Property(x => x.Description).HasColumnType("text").IsRequired();
            e.Property(x => x.RootCause).HasColumnType("text");
            e.Property(x => x.AffectedAmount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<FinancialCaseLink>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.FinancialCaseId, x.NodeType, x.EntityId });
            e.Property(x => x.Role).HasMaxLength(100).IsRequired();
            e.Property(x => x.Label).HasMaxLength(300).IsRequired();
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.HasOne<FinancialCase>().WithMany().HasForeignKey(x => x.FinancialCaseId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FinancialGraphRelation>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.FromNodeType, x.FromEntityId, x.ToNodeType, x.ToEntityId, x.RelationType }).IsUnique();
        });

        modelBuilder.Entity<WebhookEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ConnectionId, x.ExternalEventId }).IsUnique();
            e.Property(x => x.ExternalEventId).HasMaxLength(200).IsRequired();
            e.Property(x => x.EventType).HasMaxLength(200).IsRequired();
            e.Property(x => x.PayloadJson).HasColumnType("text").IsRequired();
            e.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<IntegrationConnection>().WithMany().HasForeignKey(x => x.ConnectionId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
