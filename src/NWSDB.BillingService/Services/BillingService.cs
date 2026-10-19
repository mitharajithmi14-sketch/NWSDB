using Microsoft.EntityFrameworkCore;
using NWSDB.BillingService.Data;
using NWSDB.BillingService.Models;
using NWSDB.Shared.Dtos;

namespace NWSDB.BillingService.Services;

public enum BillChangeResult { Success, NotFound, Duplicate, AlreadyPaid }

public interface IBillingService
{
    Task<BillDto?> GetCurrentBillAsync(string accountNo);
    Task<IReadOnlyList<BillDto>> GetBillHistoryAsync(string accountNo);
    Task<IReadOnlyList<BillDto>> GetAllAsync();
    Task<BillDto?> GetByIdAsync(int billId);
    Task<(BillChangeResult Result, BillDto? Bill)> CreateAsync(CreateBillRequest request);
    Task<(BillChangeResult Result, BillDto? Bill)> UpdateAsync(int billId, UpdateBillRequest request);
    Task<BillChangeResult> DeleteAsync(int billId);
    Task<(BillChangeResult Result, BillDto? Bill)> MarkPaidAsync(int billId);
}

public class BillingService : IBillingService
{
    private readonly BillingDbContext _db;
    private readonly ITariffCalculator _tariff;

    public BillingService(BillingDbContext db, ITariffCalculator tariff)
    {
        _db = db;
        _tariff = tariff;
    }

    public async Task<BillDto?> GetCurrentBillAsync(string accountNo)
    {
        var bill = await _db.Bills.Where(b => b.AccountNo == accountNo)
            .OrderByDescending(b => b.Month).FirstOrDefaultAsync();
        return bill is null ? null : ToDto(bill);
    }

    public async Task<IReadOnlyList<BillDto>> GetBillHistoryAsync(string accountNo) =>
        (await _db.Bills.Where(b => b.AccountNo == accountNo)
            .OrderByDescending(b => b.Month).ToListAsync())
        .Select(ToDto).ToList();

    public async Task<IReadOnlyList<BillDto>> GetAllAsync() =>
        (await _db.Bills.OrderByDescending(b => b.Month).ThenBy(b => b.AccountNo).ToListAsync())
        .Select(ToDto).ToList();

    public async Task<BillDto?> GetByIdAsync(int billId)
    {
        var bill = await _db.Bills.FindAsync(billId);
        return bill is null ? null : ToDto(bill);
    }

    public async Task<(BillChangeResult Result, BillDto? Bill)> CreateAsync(CreateBillRequest request)
    {
        if (await _db.Bills.AnyAsync(b => b.AccountNo == request.AccountNo && b.Month == request.Month))
            return (BillChangeResult.Duplicate, null);

        var bill = new Bill
        {
            AccountNo = request.AccountNo,
            Month = request.Month,
            UnitsUsed = request.UnitsUsed,
            Amount = _tariff.Calculate(request.UnitsUsed),
            DueDate = request.DueDate,
            Status = BillStatus.Unpaid
        };
        _db.Bills.Add(bill);
        await _db.SaveChangesAsync();
        return (BillChangeResult.Success, ToDto(bill));
    }

    public async Task<(BillChangeResult Result, BillDto? Bill)> UpdateAsync(int billId, UpdateBillRequest request)
    {
        var bill = await _db.Bills.FindAsync(billId);
        if (bill is null) return (BillChangeResult.NotFound, null);

        bill.Amount = request.Amount;
        bill.DueDate = request.DueDate;
        bill.Status = request.Status;
        await _db.SaveChangesAsync();
        return (BillChangeResult.Success, ToDto(bill));
    }

    public async Task<BillChangeResult> DeleteAsync(int billId)
    {
        var bill = await _db.Bills.FindAsync(billId);
        if (bill is null) return BillChangeResult.NotFound;

        _db.Bills.Remove(bill);
        await _db.SaveChangesAsync();
        return BillChangeResult.Success;
    }

    public async Task<(BillChangeResult Result, BillDto? Bill)> MarkPaidAsync(int billId)
    {
        var bill = await _db.Bills.FindAsync(billId);
        if (bill is null) return (BillChangeResult.NotFound, null);
        if (bill.Status == BillStatus.Paid) return (BillChangeResult.AlreadyPaid, ToDto(bill));

        bill.Status = BillStatus.Paid;
        await _db.SaveChangesAsync();
        return (BillChangeResult.Success, ToDto(bill));
    }

    private static BillDto ToDto(Bill b) =>
        new(b.BillId, b.AccountNo, b.Month, b.UnitsUsed, b.Amount, b.DueDate, b.Status);
}
