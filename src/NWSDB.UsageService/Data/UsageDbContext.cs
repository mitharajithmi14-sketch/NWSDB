using Microsoft.EntityFrameworkCore;
using NWSDB.UsageService.Models;

namespace NWSDB.UsageService.Data;

public class UsageDbContext : DbContext
{
    public UsageDbContext(DbContextOptions<UsageDbContext> options) : base(options) { }

    public DbSet<MeterReading> Readings => Set<MeterReading>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MeterReading>().HasKey(r => r.ReadingId);   // primary key
        modelBuilder.Entity<MeterReading>().Property(r => r.AccountNo).HasMaxLength(20);
        modelBuilder.Entity<MeterReading>().Property(r => r.Month).HasMaxLength(7);
        modelBuilder.Entity<MeterReading>().HasIndex(r => new { r.AccountNo, r.Month }).IsUnique();
    }
}

/// <summary>Adds six months of demo meter readings the first time the service starts.</summary>
public static class SeedData
{
    public static void Initialize(UsageDbContext db)
    {
        db.Database.EnsureCreated();
        if (db.Readings.Any()) return;

        var months = new[] { "2026-04", "2026-05", "2026-06", "2026-07", "2026-08", "2026-09" };
        var data = new Dictionary<string, (int Start, int[] Units)>
        {
            ["1001"] = (1200, new[] { 18, 20, 17, 22, 19, 24 }),
            ["1002"] = (640, new[] { 9, 10, 8, 11, 10, 9 }),
            ["1003"] = (910, new[] { 12, 14, 13, 15, 12, 16 })
        };

        foreach (var (accountNo, (start, units)) in data)
        {
            var meter = start;
            for (var i = 0; i < months.Length; i++)
            {
                meter += units[i];
                db.Readings.Add(new MeterReading
                {
                    AccountNo = accountNo,
                    Month = months[i],
                    MeterValue = meter,
                    UnitsUsed = units[i]
                });
            }
        }
        db.SaveChanges();
    }
}
