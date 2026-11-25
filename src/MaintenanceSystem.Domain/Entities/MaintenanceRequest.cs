using MaintenanceSystem.Domain.Enums;
using MaintenanceSystem.Domain.Exceptions;

namespace MaintenanceSystem.Domain.Entities;

public class MaintenanceRequest
{
    public int Id { get; set; }
    public int UnitId { get; set; }
    public int TenantId { get; set; }
    public int? TechnicianId { get; private set; }
    public string Category { get; set; } = string.Empty;
    public RequestPriority Priority { get; set; }
    public RequestStatus Status { get; private set; } = RequestStatus.Abierta;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; private set; }

    public Unit Unit { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
    public Technician? Technician { get; set; }
    public ICollection<Visit> Visits { get; set; } = new List<Visit>();

    private MaintenanceRequest() { }

    public static MaintenanceRequest Create(
        Unit unit,
        int tenantId,
        string category,
        RequestPriority priority,
        string description,
        DateTime now)
    {
        var activeLeases = unit.Leases.Where(l => l.IsActiveAt(now)).ToList();

        if (activeLeases.Count > 1)
            throw new DomainException("La unidad presenta múltiples contratos vigentes; estado inconsistente.", 409);

        var activeLease = activeLeases.FirstOrDefault();
        if (activeLease is null || activeLease.TenantId != tenantId)
            throw new DomainException("Solo el inquilino con contrato vigente en esta unidad puede crear una solicitud.", 403);

        return new MaintenanceRequest
        {
            UnitId = unit.Id,
            TenantId = tenantId,
            Category = category.Trim(),
            Priority = priority,
            Status = RequestStatus.Abierta,
            Description = description.Trim(),
            CreatedAt = now
        };
    }

    public Visit RegisterVisit(Technician technician, DateTime visitDate, string notes, decimal cost)
    {
        if (Status is RequestStatus.Cerrada or RequestStatus.Cancelada)
            throw new DomainException($"No se pueden registrar visitas en una solicitud en estado '{Status}'.", 409);

        var visit = new Visit(Id, technician.Id, visitDate, notes.Trim(), cost);
        Visits.Add(visit);
        return visit;
    }

    public void ChangeStatus(RequestStatus newStatus, Technician? technician, DateTime now)
    {
        if (Status == newStatus)
            throw new DomainException($"La solicitud ya se encuentra en estado '{Status}'.", 409);

        var isValidTransition = (Status, newStatus) switch
        {
            (RequestStatus.Abierta, RequestStatus.Asignada) => true,
            (RequestStatus.Asignada, RequestStatus.EnProgreso) => true,
            (RequestStatus.EnProgreso, RequestStatus.Cerrada) => true,
            (RequestStatus.Abierta, RequestStatus.Cancelada) => true,
            (RequestStatus.Asignada, RequestStatus.Cancelada) => true,
            _ => false
        };

        if (!isValidTransition)
            throw new DomainException($"Transición de estado inválida: '{Status}' -> '{newStatus}'.", 409);

        if (newStatus == RequestStatus.Asignada)
        {
            if (technician is null)
                throw new DomainException("Se requiere especificar un técnico para pasar la solicitud a 'Asignada'.", 400);

            if (!technician.IsActive)
                throw new DomainException("El técnico asignado debe estar activo.", 400);

            TechnicianId = technician.Id;
            Technician = technician;
        }

        if (newStatus == RequestStatus.Cerrada)
        {
            if (!Visits.Any())
                throw new DomainException("Para cerrar la solicitud debe tener al menos una visita registrada.", 409);

            ClosedAt = now;
        }

        Status = newStatus;
    }
}