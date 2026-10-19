using Microsoft.EntityFrameworkCore;
using NWSDB.NotificationService.Data;
using NWSDB.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.AddNwsdbCommon("NWSDB Notification Service");
builder.Services.AddDbContext<NotificationDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("NotificationDb")));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<NotificationDbContext>().Database.EnsureCreated();
}

app.UseNwsdbCommon();
app.Run();
