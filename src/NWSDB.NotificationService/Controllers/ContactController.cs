using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NWSDB.NotificationService.Data;
using NWSDB.NotificationService.Models;
using NWSDB.Shared;
using NWSDB.Shared.Dtos;

namespace NWSDB.NotificationService.Controllers;

[ApiController]
[Route("api/contact")]
public class ContactController : ControllerBase
{
    private readonly NotificationDbContext _db;
    public ContactController(NotificationDbContext db) => _db = db;

    /// <summary>Saves a message from the website contact form. No login needed.</summary>
    [HttpPost]
    public async Task<ActionResult<MessageResponse>> Send(ContactRequest request)
    {
        _db.ContactMessages.Add(new ContactMessage
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            Message = request.Message.Trim(),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return Ok(new MessageResponse("Message sent. We will reply by email."));
    }

    /// <summary>All customer messages, newest first (NWSDB staff only).</summary>
    [HttpGet]
    [Authorize(Policy = ServiceExtensions.AdminPolicy)]
    public async Task<IEnumerable<ContactMessageDto>> GetAll() =>
        await _db.ContactMessages.OrderByDescending(m => m.CreatedAt)
            .Select(m => new ContactMessageDto(m.Id, m.Name, m.Email, m.Message, m.CreatedAt))
            .ToListAsync();
}
