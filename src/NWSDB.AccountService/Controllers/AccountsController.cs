using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NWSDB.AccountService.Data;
using NWSDB.Shared;
using NWSDB.Shared.Dtos;

namespace NWSDB.AccountService.Controllers;

[ApiController]
[Route("api/accounts")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly AccountDbContext _db;
    public AccountsController(AccountDbContext db) => _db = db;

    /// <summary>Returns one water account. Customers can only read their own account.</summary>
    [HttpGet("{accountNo}")]
    public async Task<ActionResult<AccountDto>> Get(string accountNo)
    {
        if (!User.CanAccess(accountNo)) return Forbid();

        var a = await _db.Accounts.Include(x => x.Customer).FirstOrDefaultAsync(x => x.AccountNo == accountNo);
        if (a == null) return NotFound(new MessageResponse("Account not found."));

        return new AccountDto(a.AccountNo, a.Customer?.FullName ?? "", a.Customer?.Email ?? "", a.Customer?.Phone ?? "",
            a.Customer?.Address ?? "", a.ConnectionType, a.Status, a.PasswordHash != null);
    }

    /// <summary>Lists every water account (NWSDB staff only).</summary>
    [HttpGet]
    [Authorize(Policy = ServiceExtensions.AdminPolicy)]
    public async Task<IEnumerable<AccountDto>> GetAll() =>
        await _db.Accounts.Include(x => x.Customer).OrderBy(x => x.AccountNo)
            .Select(a => new AccountDto(a.AccountNo, a.Customer!.FullName, a.Customer.Email, a.Customer.Phone,
                a.Customer.Address, a.ConnectionType, a.Status, a.PasswordHash != null))
            .ToListAsync();
}
