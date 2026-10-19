using Microsoft.EntityFrameworkCore;
using NWSDB.Shared;
using NWSDB.UsageService.Data;
using NWSDB.UsageService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddNwsdbCommon("NWSDB Usage Service");
builder.Services.AddDbContext<UsageDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("UsageDb")));
builder.Services.AddScoped<IUsageService, UsageService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    SeedData.Initialize(scope.ServiceProvider.GetRequiredService<UsageDbContext>());
}

app.UseNwsdbCommon();
app.Run();
