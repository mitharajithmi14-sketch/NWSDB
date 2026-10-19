using Microsoft.EntityFrameworkCore;
using NWSDB.AccountService.Data;
using NWSDB.AccountService.Services;
using NWSDB.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.AddNwsdbCommon("NWSDB Account Service");
builder.Services.AddDbContext<AccountDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("AccountDb")));
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    SeedData.Initialize(scope.ServiceProvider.GetRequiredService<AccountDbContext>());
}

app.UseNwsdbCommon();
app.Run();
