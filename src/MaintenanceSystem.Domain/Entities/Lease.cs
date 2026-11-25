namespace MaintenanceSystem.Domain.Entities;

public class Lease
{
    public int Id { get; set; }
    public int UnitId { get; set; }
    public int TenantId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal MonthlyRent { get; set; }

    public Unit Unit { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;

    public bool IsActiveAt(DateTime referenceDate)
    {
        var date = referenceDate.Date;
        return StartDate.Date <= date && (!EndDate.HasValue || EndDate.Value.Date >= date);
    }

    public bool OverlapsWith(DateTime otherStart, DateTime? otherEnd)
    {
        var startA = StartDate.Date;
        var endA = EndDate?.Date ?? DateTime.MaxValue.Date;
        var startB = otherStart.Date;
        var endB = otherEnd?.Date ?? DateTime.MaxValue.Date;

        return startA <= endB && startB <= endA;
    }
}