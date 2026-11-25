using MaintenanceSystem.Domain.Entities;
using MaintenanceSystem.Domain.Enums;
using MaintenanceSystem.Domain.Exceptions;

namespace MaintenanceSystem.UnitTests;

public class MaintenanceBusinessRulesTests
{
    [Fact]
    public void CreateRequest_WithActiveLeaseTenant_Succeeds()
    {
        var now = DateTime.UtcNow;
        var unit = new Unit { Id = 1, Number = "101" };
        unit.Leases.Add(new Lease { UnitId = 1, TenantId = 10, StartDate = now.AddMonths(-2), EndDate = now.AddMonths(4) });

        var request = MaintenanceRequest.Create(unit, 10, "Plomería", RequestPriority.Alta, "Fuga en lavabo", now);

        Assert.Equal(RequestStatus.Abierta, request.Status);
        Assert.Equal(10, request.TenantId);
    }

    [Fact]
    public void CreateRequest_WithoutActiveLease_ThrowsForbidden()
    {
        var now = DateTime.UtcNow;
        var unit = new Unit { Id = 1, Number = "101" };
        unit.Leases.Add(new Lease { UnitId = 1, TenantId = 10, StartDate = now.AddMonths(-10), EndDate = now.AddMonths(-1) });

        var ex = Assert.Throws<DomainException>(() =>
            MaintenanceRequest.Create(unit, 10, "Plomería", RequestPriority.Media, "Fuga", now));

        Assert.Equal(403, ex.StatusCode);
    }

    [Fact]
    public void ChangeStatus_ToAsignada_WithInactiveTechnician_ThrowsBadRequest()
    {
        var now = DateTime.UtcNow;
        var unit = new Unit { Id = 1 };
        unit.Leases.Add(new Lease { UnitId = 1, TenantId = 10, StartDate = now.AddDays(-5) });
        var req = MaintenanceRequest.Create(unit, 10, "Electricidad", RequestPriority.Alta, "Corto", now);
        var inactiveTech = new Technician { Id = 5, Name = "Inactivo", IsActive = false };

        var ex = Assert.Throws<DomainException>(() => req.ChangeStatus(RequestStatus.Asignada, inactiveTech, now));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public void ChangeStatus_ToCerrada_WithoutVisits_ThrowsConflict()
    {
        var now = DateTime.UtcNow;
        var unit = new Unit { Id = 1 };
        unit.Leases.Add(new Lease { UnitId = 1, TenantId = 10, StartDate = now.AddDays(-5) });
        var req = MaintenanceRequest.Create(unit, 10, "General", RequestPriority.Baja, "Puerta", now);
        var activeTech = new Technician { Id = 1, Name = "Activo", IsActive = true };

        req.ChangeStatus(RequestStatus.Asignada, activeTech, now);
        req.ChangeStatus(RequestStatus.EnProgreso, null, now);

        var ex = Assert.Throws<DomainException>(() => req.ChangeStatus(RequestStatus.Cerrada, null, now));
        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public void ChangeStatus_InvalidTransition_ThrowsConflict()
    {
        var now = DateTime.UtcNow;
        var unit = new Unit { Id = 1 };
        unit.Leases.Add(new Lease { UnitId = 1, TenantId = 10, StartDate = now.AddDays(-5) });
        var req = MaintenanceRequest.Create(unit, 10, "General", RequestPriority.Baja, "Puerta", now);

        var ex = Assert.Throws<DomainException>(() => req.ChangeStatus(RequestStatus.Cerrada, null, now));
        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public void RegisterVisit_WithNegativeCost_ThrowsBadRequest()
    {
        var now = DateTime.UtcNow;
        var unit = new Unit { Id = 1 };
        unit.Leases.Add(new Lease { UnitId = 1, TenantId = 10, StartDate = now.AddDays(-5) });
        var req = MaintenanceRequest.Create(unit, 10, "General", RequestPriority.Baja, "Puerta", now);
        var tech = new Technician { Id = 1, Name = "Activo", IsActive = true };

        var ex = Assert.Throws<DomainException>(() => req.RegisterVisit(tech, now, "Nota", -50m));
        Assert.Equal(400, ex.StatusCode);
    }
}