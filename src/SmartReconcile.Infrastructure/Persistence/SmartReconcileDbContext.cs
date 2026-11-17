using Microsoft.EntityFrameworkCore;
using SmartReconcile.Domain.Entities;

namespace SmartReconcile.Infrastructure.Persistence;

public sealed class SmartReconcileDbContext(DbContextOptions<SmartReconcileDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Discrepancy> Discrepancies => Set<Discrepancy>();

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
    }
}