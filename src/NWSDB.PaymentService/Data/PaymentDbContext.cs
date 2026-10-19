using Microsoft.EntityFrameworkCore;
using NWSDB.PaymentService.Models;

namespace NWSDB.PaymentService.Data;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>().Property(p => p.ReceiptNo).HasMaxLength(20);
        modelBuilder.Entity<Payment>().Property(p => p.AccountNo).HasMaxLength(20);
        modelBuilder.Entity<Payment>().Property(p => p.Amount).HasPrecision(18, 2);  // LKR
        modelBuilder.Entity<Payment>().HasIndex(p => p.ReceiptNo).IsUnique();
    }
}
