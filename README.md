# NWSDB – Service-Oriented Bill Payment and Usage System

CSE5013 Service Oriented Computing – student project. Not an official NWSDB system.

## What is inside

| Project | Port | What it does |
|---|---|---|
| NWSDB.ApiGateway | 5000 | YARP API gateway + the NWSDB customer website (wwwroot) |
| NWSDB.AccountService | 5001 | Register, login (JWT), water accounts |
| NWSDB.BillingService | 5002 | Current bill, bill history, create/update/delete bills, tariff |
| NWSDB.UsageService | 5003 | Monthly meter readings and units used |
| NWSDB.PaymentService | 5004 | Pay a bill, receipt numbers (calls Billing + Notification) |
| NWSDB.NotificationService | 5005 | SMS/email receipts (logged), contact messages |
| NWSDB.Shared | – | DTOs and shared start-up code (JWT, Swagger, CORS) |
| NWSDB.WindowsClient | – | Windows Forms desktop client (REST customer tab + SOAP staff CRUD tab) |
| NWSDB.Tests | – | xUnit unit tests |

**SOAP web service:** the Billing service also exposes a SOAP endpoint (SoapCore) at
http://localhost:5002/soap/BillingService.asmx (WSDL: add `?wsdl`). Its operations GetBills, GetBill,
CreateBill, UpdateBill and DeleteBill use ADO.NET (`System.Data.SqlClient`) in `Data/BillSqlRepository.cs`.

Each service has its own SQL Server Express database (NWSDB_Account, NWSDB_Billing, NWSDB_Usage, NWSDB_Payment, NWSDB_Notification).
They are created automatically, with demo data, the first time each service starts (EF Core `EnsureCreated`).
The connection strings are in each service's `appsettings.json` and point to `.\SQLEXPRESS` with Windows authentication.
If your SQL Server instance has a different name, change `Server=` in those five files.
To see the tables: View → Server Explorer → right-click Data Connections → Add Connection → Server name `.\SQLEXPRESS` → choose a NWSDB_ database.

## Run in Visual Studio 2022

1. Extract the zip, then open `NWSDB.sln` (needs the .NET 8 SDK and the "ASP.NET and web development" workload). NuGet packages restore automatically.
2. Start all six projects together - choose ONE way:
   - **Easiest (VS 2022 17.11 or later):** in the toolbar, open the start-up dropdown next to the green Start button and pick **NWSDB - all services + website** (from `NWSDB.slnLaunch`).
   - **Any VS 2022:** right-click the solution → **Configure Startup Projects…** → **Multiple startup projects** → set **Start** for ApiGateway, AccountService, BillingService, UsageService, PaymentService, NotificationService and WindowsClient → **OK**.
   - **Without Visual Studio:** double-click `run-all.bat`.
3. Press **F5**. The website opens at http://localhost:5000 and each service opens its Swagger page (e.g. http://localhost:5002/swagger).
4. Run the unit tests: **Test → Run All Tests** (Test Explorer).

If you see "A project with an Output Type of Class Library cannot be started directly", the start-up project is NWSDB.Shared or NWSDB.Tests. Do step 2 again.

## Run with the .NET CLI

```
dotnet build
dotnet run --project src/NWSDB.AccountService
dotnet run --project src/NWSDB.BillingService
dotnet run --project src/NWSDB.UsageService
dotnet run --project src/NWSDB.PaymentService
dotnet run --project src/NWSDB.NotificationService
dotnet run --project src/NWSDB.ApiGateway
dotnet test
```
(Each `dotnet run` in its own terminal.)

## Run with Docker

```
docker compose up --build
```
Website: http://localhost:8080

## Demo logins

| User | Password | Role |
|---|---|---|
| 1001 (K. Perera) | water1001 | Customer |
| 1002 (S. Fernando) | water1002 | Customer |
| admin | admin123 | NWSDB staff (Admin Dashboard) |

Account **1003** exists but is not registered – use it to test registration.

The tariff in `TariffCalculator.cs` is a simplified sample (LKR 300 service charge; 60/80/110 per unit in blocks), not the official NWSDB tariff.
