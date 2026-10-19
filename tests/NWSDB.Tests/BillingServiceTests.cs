using Microsoft.EntityFrameworkCore;
using NWSDB.BillingService.Data;
using NWSDB.BillingService.Models;
using NWSDB.BillingService.Services;
using NWSDB.Shared.Dtos;
using Xunit;

namespace NWSDB.Tests;

public class BillingServiceTests
{
    private static BillingDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new BillingDbContext(options);
        db.Bills.AddRange(
            new Bill { AccountNo = "1001", Month = "2026-08", UnitsUsed = 19, Amount = 1830, DueDate = new DateOnly(2026, 9, 20), Status = BillStatus.Paid },
            new Bill { AccountNo = "1001", Month = "2026-09", UnitsUsed = 24, Amount = 2140, DueDate = new DateOnly(2026, 10, 20), Status = BillStatus.Unpaid });
        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task GetCurrentBill_ReturnsLatestMonth()
    {
        var service = new NWSDB.BillingService.Services.BillingService(NewDb(), new TariffCalculator());
        var bill = await service.GetCurrentBillAsync("1001");
        Assert.NotNull(bill);
        Assert.Equal("2026-09", bill!.Month);
        Assert.Equal(2140m, bill.Amount);
    }

    [Fact]
    public async Task GetCurrentBill_UnknownAccount_ReturnsNull()
    {
        var service = new NWSDB.BillingService.Services.BillingService(NewDb(), new TariffCalculator());
        Assert.Null(await service.GetCurrentBillAsync("9999"));
    }

    [Fact]
    public async Task Create_CalculatesAmountFromTariff()
    {
        var service = new NWSDB.BillingService.Services.BillingService(NewDb(), new TariffCalculator());
        var (result, bill) = await service.CreateAsync(new CreateBillRequest("1001", "2026-10", 15, new DateOnly(2026, 11, 20)));
        Assert.Equal(BillChangeResult.Success, result);
        Assert.Equal(1300m, bill!.Amount);
        Assert.Equal(BillStatus.Unpaid, bill.Status);
    }

    [Fact]
    public async Task Create_SameMonthTwice_ReturnsDuplicate()
    {
        var service = new NWSDB.BillingService.Services.BillingService(NewDb(), new TariffCalculator());
        var (result, _) = await service.CreateAsync(new CreateBillRequest("1001", "2026-09", 10, new DateOnly(2026, 10, 20)));
        Assert.Equal(BillChangeResult.Duplicate, result);
    }

    [Fact]
    public async Task MarkPaid_PaidBill_ReturnsAlreadyPaid()
    {
        var db = NewDb();
        var service = new NWSDB.BillingService.Services.BillingService(db, new TariffCalculator());
        var paidBill = db.Bills.First(b => b.Status == BillStatus.Paid);
        var (result, _) = await service.MarkPaidAsync(paidBill.BillId);
        Assert.Equal(BillChangeResult.AlreadyPaid, result);
    }
}
