FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY MaintenanceSystem.slnx ./
COPY src/MaintenanceSystem.Domain/MaintenanceSystem.Domain.csproj src/MaintenanceSystem.Domain/
COPY src/MaintenanceSystem.Application/MaintenanceSystem.Application.csproj src/MaintenanceSystem.Application/
COPY src/MaintenanceSystem.Infrastructure/MaintenanceSystem.Infrastructure.csproj src/MaintenanceSystem.Infrastructure/
COPY src/MaintenanceSystem.Api/MaintenanceSystem.Api.csproj src/MaintenanceSystem.Api/
COPY tests/MaintenanceSystem.UnitTests/MaintenanceSystem.UnitTests.csproj tests/MaintenanceSystem.UnitTests/

RUN dotnet restore

COPY . .
RUN dotnet publish src/MaintenanceSystem.Api/MaintenanceSystem.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MaintenanceSystem.Api.dll"]