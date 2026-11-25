using MaintenanceSystem.Domain.Enums;

namespace MaintenanceSystem.Domain.Entities;

public class Unit
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public string Number { get; set; } = string.Empty;
    public int Floor { get; set; }
    public UnitType Type { get; set; }

    public Property Property { get; set; } = null!;
    public ICollection<Lease> Leases { get; set; } = new List<Lease>();
    public ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();
}