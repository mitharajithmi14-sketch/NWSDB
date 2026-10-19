using Microsoft.EntityFrameworkCore;
using NWSDB.Shared.Dtos;
using NWSDB.UsageService.Data;
using NWSDB.UsageService.Models;

namespace NWSDB.UsageService.Services;

public interface IUsageService
{
    Task<IReadOnlyList<UsageDto>> GetRecentAsync(string accountNo, int months);
    Task<UsageDto?> AddReadingAsync(CreateReadingRequest request);
}

public class UsageService : IUsageService
{
    private readonly UsageDbContext _db;
    public UsageService(UsageDbContext db) => _db = db;

    /// <summary>Returns the last <paramref name="months"/> readings, oldest first (ready for a chart).</summary>
    public async Task<IReadOnlyList<UsageDto>> GetRecentAsync(string accountNo, int months)
    {
        var recent = await _db.Readings.Where(r => r.AccountNo == accountNo)
            .OrderByDescending(r => r.Month).Take(months).ToListAsync();
        return recent.OrderBy(r => r.Month).Select(ToDto).ToList();
    }

    /// <summary>Adds a reading. Returns null when a reading already exists for that month.</summary>
    public async Task<UsageDto?> AddReadingAsync(CreateReadingRequest request)
    {
        if (await _db.Readings.AnyAsync(r => r.AccountNo == request.AccountNo && r.Month == request.Month))
            return null;

        var reading = new MeterReading
        {
            AccountNo = request.AccountNo,
            Month = request.Month,
            MeterValue = request.MeterValue,
            UnitsUsed = request.UnitsUsed
        };
        _db.Readings.Add(reading);
        await _db.SaveChangesAsync();
        return ToDto(reading);
    }

    private static UsageDto ToDto(MeterReading r) => new(r.ReadingId, r.AccountNo, r.Month, r.MeterValue, r.UnitsUsed);
}
