using System.Text.Json.Serialization;
using MaintenanceSystem.Api.Middleware;
using MaintenanceSystem.Application.Interfaces;
using MaintenanceSystem.Application.Services;
using MaintenanceSystem.Infrastructure.Persistence;
using MaintenanceSystem.Infrastructure.Repositories;
using MaintenanceSystem.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'DefaultConnection'.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<IMaintenanceRepository, MaintenanceRepository>();
builder.Services.AddScoped<MaintenanceService>();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

// Habilitar Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Maintenance API v1");
    c.RoutePrefix = "swagger";
});

app.MapControllers();

// Aplicar migraciones pendientes y ejecutar seed al iniciar
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    for (int attempt = 1; attempt <= 10; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            await DatabaseSeeder.SeedAsync(db);
            logger.LogInformation("Base de datos migrada y poblada correctamente.");
            break;
        }
        catch (Exception ex) when (attempt < 10)
        {
            logger.LogWarning(ex, "Esperando disponibilidad de SQL Server (intento {Attempt}/10)...", attempt);
            await Task.Delay(3000);
        }
    }
}

app.Run();