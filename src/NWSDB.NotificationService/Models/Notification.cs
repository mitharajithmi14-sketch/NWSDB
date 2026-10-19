namespace NWSDB.NotificationService.Models;

public class Notification
{
    public int NotificationId { get; set; }
    public string AccountNo { get; set; } = string.Empty;
    /// <summary>SMS or Email.</summary>
    public string Channel { get; set; } = "SMS";
    public string Message { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}

public class ContactMessage
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
