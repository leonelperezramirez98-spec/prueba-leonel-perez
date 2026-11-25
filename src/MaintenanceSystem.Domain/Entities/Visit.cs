using MaintenanceSystem.Domain.Exceptions;

namespace MaintenanceSystem.Domain.Entities;

public class Visit
{
    public int Id { get; set; }
    public int MaintenanceRequestId { get; set; }
    public int TechnicianId { get; set; }
    public DateTime VisitDate { get; set; }
    public string Notes { get; set; } = string.Empty;
    public decimal Cost { get; private set; }

    public MaintenanceRequest MaintenanceRequest { get; set; } = null!;
    public Technician Technician { get; set; } = null!;

    private Visit() { }

    public Visit(int maintenanceRequestId, int technicianId, DateTime visitDate, string notes, decimal cost)
    {
        if (cost < 0)
            throw new DomainException("El costo de la visita no puede ser negativo.", 400);

        MaintenanceRequestId = maintenanceRequestId;
        TechnicianId = technicianId;
        VisitDate = visitDate;
        Notes = notes;
        Cost = cost;
    }
}