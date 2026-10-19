using Microsoft.EntityFrameworkCore;
using NWSDB.BillingService.Models;
using NWSDB.BillingService.Services;

namespace NWSDB.BillingService.Data;

public class BillingDbContext : DbContext
{
    public BillingDbContext(DbContextOptions<BillingDbContext> options) : base(options) { }

    public DbSet<Bill> Bills => Set<Bill>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Bill>().Property(b => b.AccountNo).HasMaxLength(20);
        modelBuilder.Entity<Bill>().Property(b => b.Month).HasMaxLength(7);          // e.g. 2026-09
        modelBuilder.Entity<Bill>().Property(b => b.Status).HasMaxLength(20);
        modelBuilder.Entity<Bill>().Property(b => b.Amount).HasPrecision(18, 2);     // LKR
        modelBuilder.Entity<Bill>().HasIndex(b => new { b.AccountNo, b.Month }).IsUnique();
    }
}

/// <summary>Adds six months of demo bills for each account the first time the service starts.</summary>
public static class SeedData
{
    public static void Initialize(BillingDbContext db, ITariffCalculator tariff)
    {
        db.Database.EnsureCreated();
        if (db.Bills.Any()) return;

        var months = new[] { "2026-04", "2026-05", "2026-06", "2026-07", "2026-08", "2026-09" };
        var units = new Dictionary<string, int[]>
        {
            ["1001"] = new[] { 18, 20, 17, 22, 19, 24 },
            ["1002"] = new[] { 9, 10, 8, 11, 10, 9 },
            ["1003"] = new[] { 12, 14, 13, 15, 12, 16 }
        };

        foreach (var (accountNo, used) in units)
        {
            for (var i = 0; i < months.Length; i++)
            {
                var month = DateOnly.ParseExact(months[i] + "-01", "yyyy-MM-dd");
                db.Bills.Add(new Bill
                {
                    AccountNo = accountNo,
                    Month = months[i],
                    UnitsUsed = used[i],
                    Amount = tariff.Calculate(used[i]),
                    DueDate = month.AddMonths(1).AddDays(19),            // 20th of the next month
                    Status = i < months.Length - 1 ? BillStatus.Paid : BillStatus.Unpaid
                });
            }
        }
        db.SaveChanges();
    }
}
