# Build stage: restore and publish the service (build context = the src folder)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish NWSDB.UsageService/NWSDB.UsageService.csproj -c Release -o /app/publish

# Run stage: small ASP.NET Core runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "NWSDB.UsageService.dll"]
