using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NWSDB.BillingService.Services;
using NWSDB.Shared;
using NWSDB.Shared.Dtos;

namespace NWSDB.BillingService.Controllers;

[ApiController]
[Route("api/bills")]
[Authorize]
public class BillsController : ControllerBase
{
    private const string AccountNotFound = "Account not found. Check the number on your bill and try again.";
    private readonly IBillingService _billing;

    public BillsController(IBillingService billing) => _billing = billing;

    /// <summary>Current bill for an account. Public, so the quick bill check and partners can use it.</summary>
    [HttpGet("{accountNo}/current")]
    [AllowAnonymous]
    public async Task<ActionResult<BillDto>> GetCurrent(string accountNo)
    {
        var bill = await _billing.GetCurrentBillAsync(accountNo.Trim());
        return bill is null ? NotFound(new MessageResponse(AccountNotFound)) : Ok(bill);
    }

    /// <summary>All bills for an account, newest first.</summary>
    [HttpGet("{accountNo}/history")]
    public async Task<ActionResult<IEnumerable<BillDto>>> GetHistory(string accountNo)
    {
        if (!User.CanAccess(accountNo)) return Forbid();
        return Ok(await _billing.GetBillHistoryAsync(accountNo));
    }

    /// <summary>Every bill for every account (NWSDB staff only).</summary>
    [HttpGet]
    [Authorize(Policy = ServiceExtensions.AdminPolicy)]
    public async Task<IEnumerable<BillDto>> GetAll() => await _billing.GetAllAsync();

    /// <summary>Creates a bill from a meter reading. The amount is calculated from the tariff.</summary>
    [HttpPost]
    [Authorize(Policy = ServiceExtensions.AdminPolicy)]
    public async Task<IActionResult> Create(CreateBillRequest request)
    {
        var (result, bill) = await _billing.CreateAsync(request);
        return result == BillChangeResult.Duplicate
            ? Conflict(new MessageResponse("A bill for this account and month already exists."))
            : CreatedAtAction(nameof(GetCurrent), new { accountNo = bill!.AccountNo }, bill);
    }

    /// <summary>Updates the amount, due date or status of a bill (NWSDB staff only).</summary>
    [HttpPut("{billId:int}")]
    [Authorize(Policy = ServiceExtensions.AdminPolicy)]
    public async Task<IActionResult> Update(int billId, UpdateBillRequest request)
    {
        var (result, bill) = await _billing.UpdateAsync(billId, request);
        return result == BillChangeResult.NotFound ? NotFound(new MessageResponse("Bill not found.")) : Ok(bill);
    }

    /// <summary>Deletes a bill (NWSDB staff only).</summary>
    [HttpDelete("{billId:int}")]
    [Authorize(Policy = ServiceExtensions.AdminPolicy)]
    public async Task<IActionResult> Delete(int billId)
    {
        var result = await _billing.DeleteAsync(billId);
        return result == BillChangeResult.NotFound ? NotFound(new MessageResponse("Bill not found.")) : NoContent();
    }

    /// <summary>Marks a bill as paid. Called by the Payment Service.</summary>
    [HttpPut("{billId:int}/pay")]
    public async Task<IActionResult> MarkPaid(int billId)
    {
        var existing = await _billing.GetByIdAsync(billId);
        if (existing is null) return NotFound(new MessageResponse("Bill not found."));
        if (!User.CanAccess(existing.AccountNo)) return Forbid();

        var (result, bill) = await _billing.MarkPaidAsync(billId);
        return result == BillChangeResult.AlreadyPaid
            ? Conflict(new MessageResponse("This bill is already paid."))
            : Ok(bill);
    }
}
