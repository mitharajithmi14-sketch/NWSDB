@echo off
REM Starts all NWSDB services and the website, each in its own window.
REM Needs the .NET 8 SDK (installed with Visual Studio 2022).
cd /d "%~dp0"
start "Account Service :5001"      cmd /k dotnet run --project src\NWSDB.AccountService --launch-profile http
start "Billing Service :5002"      cmd /k dotnet run --project src\NWSDB.BillingService --launch-profile http
start "Usage Service :5003"        cmd /k dotnet run --project src\NWSDB.UsageService --launch-profile http
start "Payment Service :5004"      cmd /k dotnet run --project src\NWSDB.PaymentService --launch-profile http
start "Notification Service :5005" cmd /k dotnet run --project src\NWSDB.NotificationService --launch-profile http
start "Website + API Gateway :5000" cmd /k dotnet run --project src\NWSDB.ApiGateway --launch-profile http
echo Waiting for the services to start (first run takes longer)...
timeout /t 20 /nobreak > nul
start http://localhost:5000
start "Windows client" cmd /c dotnet run --project src\NWSDB.WindowsClient
