using Microsoft.EntityFrameworkCore;
using NWSDB.BillingService.Data;
using NWSDB.BillingService.Services;
using NWSDB.BillingService.Soap;
using NWSDB.Shared;
using SoapCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddNwsdbCommon("NWSDB Billing Service");
builder.Services.AddDbContext<BillingDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("BillingDb")));
builder.Services.AddSingleton<ITariffCalculator, TariffCalculator>();
builder.Services.AddScoped<IBillingService, BillingService>();

// SOAP web service (SoapCore) + ADO.NET data access with System.Data.SqlClient
builder.Services.AddSoapCore();
builder.Services.AddScoped<BillSqlRepository>();
builder.Services.AddScoped<IBillingSoapService, BillingSoapService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    SeedData.Initialize(
        scope.ServiceProvider.GetRequiredService<BillingDbContext>(),
        scope.ServiceProvider.GetRequiredService<ITariffCalculator>());
}

// SOAP endpoint - WSDL at http://localhost:5002/soap/BillingService.asmx?wsdl
((IApplicationBuilder)app).UseSoapEndpoint<IBillingSoapService>(
    "/soap/BillingService.asmx", new SoapEncoderOptions(), SoapSerializer.DataContractSerializer);

app.UseNwsdbCommon();
app.Run();
