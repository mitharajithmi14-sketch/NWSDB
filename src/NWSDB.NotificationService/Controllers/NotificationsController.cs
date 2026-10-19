using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NWSDB.NotificationService.Data;
using NWSDB.NotificationService.Models;
using NWSDB.Shared;
using NWSDB.Shared.Dtos;

namespace NWSDB.NotificationService.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly NotificationDbContext _db;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(NotificationDbContext db, ILogger<NotificationsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>Sends (logs) an SMS or email and stores it. A real SMS gateway would be called here.</summary>
    [HttpPost]
    public async Task<IActionResult> Send(NotificationRequest request)
    {
        if (!User.CanAccess(request.AccountNo)) return Forbid();

        var n = new Notification
        {
            AccountNo = request.AccountNo,
            Channel = request.Channel,
            Message = request.Message,
            SentAt = DateTime.UtcNow
        };
        _db.Notifications.Add(n);
        await _db.SaveChangesAsync();
        _logger.LogInformation("{Channel} sent to account {AccountNo}: {Message}", n.Channel, n.AccountNo, n.Message);

        var dto = new NotificationDto(n.NotificationId, n.AccountNo, n.Channel, n.Message, n.SentAt);
        return CreatedAtAction(nameof(GetByAccount), new { accountNo = n.AccountNo }, dto);
    }

    /// <summary>Messages sent to an account, newest first.</summary>
    [HttpGet("{accountNo}")]
    public async Task<ActionResult<IEnumerable<NotificationDto>>> GetByAccount(string accountNo)
    {
        if (!User.CanAccess(accountNo)) return Forbid();
        var list = await _db.Notifications.Where(n => n.AccountNo == accountNo)
            .OrderByDescending(n => n.SentAt)
            .Select(n => new NotificationDto(n.NotificationId, n.AccountNo, n.Channel, n.Message, n.SentAt))
            .ToListAsync();
        return Ok(list);
    }
}
