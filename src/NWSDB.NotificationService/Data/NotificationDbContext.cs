using Microsoft.EntityFrameworkCore;
using NWSDB.NotificationService.Models;

namespace NWSDB.NotificationService.Data;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
}
