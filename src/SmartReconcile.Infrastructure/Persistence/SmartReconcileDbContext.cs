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
    public DbSet<CaseAuditEvent> CaseAuditEvents => Set<CaseAuditEvent>();
    public DbSet<AccountingSettings> AccountingSettings => Set<AccountingSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.OrderNumber }).IsUnique();
            e.HasIndex(x => x.TenantId);
            e.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.Property(x => x.TaxRate).HasPrecision(5, 2);
            e.Property(x => x.OrderNumber).HasMaxLength(50).IsRequired();
            e.Property(x => x.CustomerName).HasMaxLength(200).IsRequired();
            e.Property(x => x.CustomerEmail).HasMaxLength(320).IsRequired();
            e.HasMany(x => x.Payments).WithOne(x => x.Order).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Payment>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.TransactionId }).IsUnique();
            e.HasIndex(x => x.TenantId);
            e.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.FeeAmount).HasPrecision(18, 2);
            e.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            e.Property(x => x.Gateway).HasMaxLength(200);
        });

        modelBuilder.Entity<Invoice>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.InvoiceNumber });
            e.HasIndex(x => x.TenantId);
            e.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => x.OrderId);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.RawText).HasColumnType("text");
        });

        modelBuilder.Entity<Discrepancy>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.Type, x.Status, x.OrderId, x.PaymentId, x.InvoiceId });
            e.HasIndex(x => x.TenantId);
            e.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
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
            e.Property(x => x.ApiKeyHash).HasMaxLength(64);
            e.Property(x => x.ShopDomain).HasMaxLength(255);
            e.Property(x => x.ShopCurrency).HasMaxLength(3).IsRequired();
            e.Property(x => x.EncryptedAccessToken).HasColumnType("text");
            e.Property(x => x.EncryptedRefreshToken).HasColumnType("text");
            e.Property(x => x.Scopes).HasMaxLength(1000).IsRequired();
            e.HasIndex(x => new { x.TenantId, x.Provider, x.ShopDomain });
            e.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FinancialCase>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.DiscrepancyId, x.Status });
            e.HasIndex(x => x.TenantId);
            e.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.CaseNumber).IsUnique();
            e.Property(x => x.CaseNumber).HasMaxLength(30);
            e.Property(x => x.Title).HasMaxLength(300).IsRequired();
            e.Property(x => x.Description).HasColumnType("text").IsRequired();
            e.Property(x => x.RootCause).HasColumnType("text");
            e.Property(x => x.AffectedAmount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<FinancialCaseLink>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.FinancialCaseId, x.NodeType, x.EntityId });
            e.HasIndex(x => x.TenantId);
            e.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.Role).HasMaxLength(100).IsRequired();
            e.Property(x => x.Label).HasMaxLength(300).IsRequired();
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.HasOne<FinancialCase>().WithMany().HasForeignKey(x => x.FinancialCaseId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FinancialGraphRelation>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.FromNodeType, x.FromEntityId, x.ToNodeType, x.ToEntityId, x.RelationType }).IsUnique();
            e.HasIndex(x => x.TenantId);
            e.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CaseAuditEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.FinancialCaseId, x.CreatedAtUtc });
            e.HasIndex(x => x.TenantId);
            e.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.Description).HasColumnType("text").IsRequired();
            e.Property(x => x.Actor).HasMaxLength(100).IsRequired();
            e.HasOne<FinancialCase>().WithMany().HasForeignKey(x => x.FinancialCaseId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AccountingSettings>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.TenantId).IsUnique();
            e.Property(x => x.Beraternummer).HasMaxLength(7).IsRequired();
            e.Property(x => x.Mandantennummer).HasMaxLength(5).IsRequired();
            e.Property(x => x.RevenueAccount19).HasMaxLength(8).IsRequired();
            e.Property(x => x.RevenueAccount7).HasMaxLength(8).IsRequired();
            e.Property(x => x.PaymentClearingAccount).HasMaxLength(8).IsRequired();
            e.Property(x => x.FeeAccount).HasMaxLength(8).IsRequired();
            e.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
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
