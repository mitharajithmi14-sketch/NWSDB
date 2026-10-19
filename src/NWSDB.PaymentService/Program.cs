using Microsoft.EntityFrameworkCore;
using NWSDB.PaymentService.Data;
using NWSDB.PaymentService.Services;
using NWSDB.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.AddNwsdbCommon("NWSDB Payment Service");
builder.Services.AddDbContext<PaymentDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("PaymentDb")));

// Typed HTTP clients for the services this service depends on
builder.Services.AddHttpClient<IBillingClient, BillingClient>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:Billing"]!));
builder.Services.AddHttpClient<INotificationClient, NotificationClient>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:Notification"]!));

builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentProcessor, PaymentProcessor>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<PaymentDbContext>().Database.EnsureCreated();
}

app.UseNwsdbCommon();
app.Run();
