using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NWSDB.Shared;
using NWSDB.Shared.Dtos;
using NWSDB.UsageService.Services;

namespace NWSDB.UsageService.Controllers;

[ApiController]
[Route("api/usage")]
[Authorize]
public class UsageController : ControllerBase
{
    private readonly IUsageService _usage;
    public UsageController(IUsageService usage) => _usage = usage;

    /// <summary>Units used (m³) for the last few months, oldest first.</summary>
    [HttpGet("{accountNo}")]
    public async Task<ActionResult<IEnumerable<UsageDto>>> Get(string accountNo, [FromQuery] int months = 6)
    {
        if (!User.CanAccess(accountNo)) return Forbid();
        var readings = await _usage.GetRecentAsync(accountNo, Math.Clamp(months, 1, 24));
        return readings.Count == 0
            ? NotFound(new MessageResponse("No usage found for this account."))
            : Ok(readings);
    }

    /// <summary>Adds a monthly meter reading (NWSDB staff only).</summary>
    [HttpPost]
    [Authorize(Policy = ServiceExtensions.AdminPolicy)]
    public async Task<IActionResult> Add(CreateReadingRequest request)
    {
        var reading = await _usage.AddReadingAsync(request);
        return reading is null
            ? Conflict(new MessageResponse("A reading for this account and month already exists."))
            : CreatedAtAction(nameof(Get), new { accountNo = reading.AccountNo }, reading);
    }
}
