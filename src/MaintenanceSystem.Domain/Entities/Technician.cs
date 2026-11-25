using MaintenanceSystem.Domain.Enums;

namespace MaintenanceSystem.Domain.Entities;

public class Technician
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TechnicianSpecialty Specialty { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<MaintenanceRequest> AssignedRequests { get; set; } = new List<MaintenanceRequest>();
    public ICollection<Visit> Visits { get; set; } = new List<Visit>();
}