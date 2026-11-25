using MaintenanceSystem.Domain.Entities;
using MaintenanceSystem.Domain.Enums;
using MaintenanceSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceSystem.Infrastructure.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Properties.AnyAsync())
            return;

        var now = DateTime.UtcNow;
        var random = new Random(42); // Semilla

        // Edificios
        var properties = new List<Property>
        {
            new() { Name = "Torre Reforma", Address = "Av. Paseo de la Reforma 250", City = "Ciudad de México" },
            new() { Name = "Residencial Providencia", Address = "Av. Rubén Darío 1120", City = "Guadalajara" },
            new() { Name = "Plaza del Río", Address = "Blvd. Agua Caliente 4500", City = "Tijuana" }
        };
        await db.Properties.AddRangeAsync(properties);
        await db.SaveChangesAsync();

        // Unidades (10 por edificio)
        var units = new List<Unit>();
        foreach (var prop in properties)
        {
            for (int i = 1; i <= 10; i++)
            {
                var floor = i <= 2 ? 0 : (i - 1) / 2;
                var type = i <= 2 ? UnitType.Local : UnitType.Departamento;
                var number = type == UnitType.Local ? $"L-0{i}" : $"{floor}0{(i % 2 == 0 ? 2 : 1)}";
                units.Add(new Unit
                {
                    PropertyId = prop.Id,
                    Number = number,
                    Floor = floor,
                    Type = type
                });
            }
        }
        await db.Units.AddRangeAsync(units);
        await db.SaveChangesAsync();

        // Inquilinos
        var tenants = Enumerable.Range(1, 20).Select(i => new Tenant
        {
            FullName = $"Inquilino {i:D2} {(i % 2 == 0 ? "Gómez" : "Martínez")}",
            Email = $"inquilino{i:D2}@ejemplo.com",
            Phone = $"664-555-{1000 + i}"
        }).ToList();
        await db.Tenants.AddRangeAsync(tenants);
        await db.SaveChangesAsync();

        // Contratos
        var leases = new List<Lease>();
        for (int i = 0; i < 20; i++)
        {
            if (i < 8)
            {
                var historicalTenant = tenants[(i + 5) % 20];
                leases.Add(new Lease
                {
                    UnitId = units[i].Id,
                    TenantId = historicalTenant.Id,
                    StartDate = now.AddMonths(-18),
                    EndDate = now.AddMonths(-8),
                    MonthlyRent = 12500m + (i * 250)
                });
            }

            leases.Add(new Lease
            {
                UnitId = units[i].Id,
                TenantId = tenants[i].Id,
                StartDate = now.AddMonths(-7),
                EndDate = i % 3 == 0 ? null : now.AddMonths(5),
                MonthlyRent = 14000m + (i * 300)
            });
        }
        await db.Leases.AddRangeAsync(leases);
        await db.SaveChangesAsync();

        // Técnicos
        var technicians = new List<Technician>
        {
            new() { Name = "Carlos Mendoza", Specialty = TechnicianSpecialty.Plomeria, IsActive = true },
            new() { Name = "Luis Herrera", Specialty = TechnicianSpecialty.Electricidad, IsActive = true },
            new() { Name = "Jorge Ramírez", Specialty = TechnicianSpecialty.General, IsActive = true },
            new() { Name = "Roberto Silva", Specialty = TechnicianSpecialty.Plomeria, IsActive = true },
            new() { Name = "Mario Delgado (Baja)", Specialty = TechnicianSpecialty.Electricidad, IsActive = false }
        };
        await db.Technicians.AddRangeAsync(technicians);
        await db.SaveChangesAsync();

        var activeTechs = technicians.Where(t => t.IsActive).ToList();
        var categories = new[] { "Plomería", "Electricidad", "Cerrajería", "Aire Acondicionado", "Pintura y Humedad" };
        var priorities = new[] { RequestPriority.Baja, RequestPriority.Media, RequestPriority.Alta, RequestPriority.Urgente };

        // Cargar unidades con sus contratos
        var occupiedUnits = await db.Units
            .Include(u => u.Leases)
            .Where(u => u.Leases.Any(l => l.StartDate <= now && (l.EndDate == null || l.EndDate >= now)))
            .ToListAsync();

        // Solicitudes + Visitas
        var requests = new List<MaintenanceRequest>();
        for (int i = 0; i < 100; i++)
        {
            var unit = occupiedUnits[i % occupiedUnits.Count];
            var activeLease = unit.Leases.First(l => l.IsActiveAt(now));
            var createdAt = now.AddDays(-random.Next(1, 180)).AddHours(-random.Next(1, 23));
            var category = categories[i % categories.Length];
            var priority = priorities[i % priorities.Length];

            var req = MaintenanceRequest.Create(
                unit,
                activeLease.TenantId,
                category,
                priority,
                $"Reporte #{i + 1}: Incidencia de {category.ToLower()} en unidad {unit.Number}.",
                createdAt
            );

            // Estados:
            // 0..19 -> Abierta (20)
            // 20..39 -> Asignada (20)
            // 40..59 -> EnProgreso (20)
            // 60..89 -> Cerrada (30)
            // 90..99 -> Cancelada (10)
            var tech = activeTechs[i % activeTechs.Count];

            if (i >= 20 && i < 40)
            {
                req.ChangeStatus(RequestStatus.Asignada, tech, createdAt.AddHours(2));
            }
            else if (i >= 40 && i < 60)
            {
                req.ChangeStatus(RequestStatus.Asignada, tech, createdAt.AddHours(2));
                req.ChangeStatus(RequestStatus.EnProgreso, null, createdAt.AddHours(6));
                req.RegisterVisit(tech, createdAt.AddHours(8), "Diagnóstico inicial y compra de refacciones.", 450m + (i * 10));
            }
            else if (i >= 60 && i < 90)
            {
                req.ChangeStatus(RequestStatus.Asignada, tech, createdAt.AddHours(1));
                req.ChangeStatus(RequestStatus.EnProgreso, null, createdAt.AddHours(4));

                var visitsCount = (i % 2) + 1;
                for (int v = 0; v < visitsCount; v++)
                {
                    var visitTime = createdAt.AddHours(6 + (v * 12));
                    if (visitTime > now) visitTime = now.AddMinutes(-30);
                    req.RegisterVisit(tech, visitTime, $"Visita técnica #{v + 1}: reparación ejecutada.", 600m + (v * 350m));
                }

                var closeTime = req.Visits.Max(v => v.VisitDate).AddHours(1);
                if (closeTime > now) closeTime = now;
                req.ChangeStatus(RequestStatus.Cerrada, null, closeTime);
            }
            else if (i >= 90)
            {
                if (i % 2 == 0)
                    req.ChangeStatus(RequestStatus.Asignada, tech, createdAt.AddHours(1));

                req.ChangeStatus(RequestStatus.Cancelada, null, createdAt.AddHours(3));
            }

            requests.Add(req);
        }

        await db.MaintenanceRequests.AddRangeAsync(requests);
        await db.SaveChangesAsync();
    }
}