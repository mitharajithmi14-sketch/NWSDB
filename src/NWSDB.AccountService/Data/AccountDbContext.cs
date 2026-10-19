using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NWSDB.AccountService.Models;

namespace NWSDB.AccountService.Data;

public class AccountDbContext : DbContext
{
    public AccountDbContext(DbContextOptions<AccountDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<WaterAccount> Accounts => Set<WaterAccount>();
    public DbSet<StaffUser> StaffUsers => Set<StaffUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WaterAccount>().HasKey(a => a.AccountNo);
        modelBuilder.Entity<WaterAccount>().Property(a => a.AccountNo).HasMaxLength(20);
        modelBuilder.Entity<StaffUser>().Property(s => s.Username).HasMaxLength(50);
        modelBuilder.Entity<WaterAccount>()
            .HasOne(a => a.Customer)
            .WithMany(c => c.Accounts)
            .HasForeignKey(a => a.CustomerId);
        modelBuilder.Entity<StaffUser>().HasIndex(s => s.Username).IsUnique();
    }
}

/// <summary>Creates the database and adds demo data the first time the service starts.</summary>
public static class SeedData
{
    public static void Initialize(AccountDbContext db)
    {
        db.Database.EnsureCreated();
        if (db.Accounts.Any()) return;

        var accountHasher = new PasswordHasher<WaterAccount>();
        var staffHasher = new PasswordHasher<StaffUser>();

        var perera = new Customer { FullName = "K. Perera", Email = "kperera@example.com", Phone = "0771234567", Address = "12 Temple Road, Maharagama" };
        var fernando = new Customer { FullName = "S. Fernando", Email = "sfernando@example.com", Phone = "0719876543", Address = "45/2 Lake Drive, Kandy" };
        var silva = new Customer { FullName = "N. Silva", Email = "nsilva@example.com", Phone = "0701112233", Address = "8 Station Road, Galle" };
        db.Customers.AddRange(perera, fernando, silva);

        var a1001 = new WaterAccount { AccountNo = "1001", Customer = perera };
        a1001.PasswordHash = accountHasher.HashPassword(a1001, "water1001");
        var a1002 = new WaterAccount { AccountNo = "1002", Customer = fernando };
        a1002.PasswordHash = accountHasher.HashPassword(a1002, "water1002");
        var a1003 = new WaterAccount { AccountNo = "1003", Customer = silva }; // not registered yet
        db.Accounts.AddRange(a1001, a1002, a1003);

        var admin = new StaffUser { Username = "admin", FullName = "NWSDB Admin", Role = "Admin" };
        admin.PasswordHash = staffHasher.HashPassword(admin, "admin123");
        db.StaffUsers.Add(admin);

        db.SaveChanges();
    }
}
