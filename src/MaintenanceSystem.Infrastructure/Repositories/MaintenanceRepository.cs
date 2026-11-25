using System.Data;
using Dapper;
using MaintenanceSystem.Application.DTOs;
using MaintenanceSystem.Application.Interfaces;
using MaintenanceSystem.Domain.Entities;
using MaintenanceSystem.Domain.Enums;
using MaintenanceSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceSystem.Infrastructure.Repositories;

public class MaintenanceRepository : IMaintenanceRepository
{
    private readonly AppDbContext _db;

    public MaintenanceRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Unit?> GetUnitWithLeasesAsync(int unitId, CancellationToken ct = default)
        => _db.Units
            .Include(u => u.Leases)
            .Include(u => u.Property)
            .FirstOrDefaultAsync(u => u.Id == unitId, ct);

    public Task<Technician?> GetTechnicianByIdAsync(int technicianId, CancellationToken ct = default)
        => _db.Technicians.FirstOrDefaultAsync(t => t.Id == technicianId, ct);

    public Task<MaintenanceRequest?> GetRequestWithDetailsAsync(int id, CancellationToken ct = default)
        => _db.MaintenanceRequests
            .Include(r => r.Unit).ThenInclude(u => u.Property)
            .Include(r => r.Tenant)
            .Include(r => r.Technician)
            .Include(r => r.Visits)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task AddRequestAsync(MaintenanceRequest request, CancellationToken ct = default)
        => await _db.MaintenanceRequests.AddAsync(request, ct);

    public async Task AddVisitAsync(Visit visit, CancellationToken ct = default)
        => await _db.Visits.AddAsync(visit, ct);

    public async Task SaveChangesWithTransactionAsync(CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task<IReadOnlyList<PropertyUnitDto>?> GetUnitsByPropertyAsync(int propertyId, DateTime now, CancellationToken ct = default)
    {
        var exists = await _db.Properties.AnyAsync(p => p.Id == propertyId, ct);
        if (!exists) return null;

        var date = now.Date;
        var units = await _db.Units
            .AsNoTracking()
            .Where(u => u.PropertyId == propertyId)
            .OrderBy(u => u.Floor).ThenBy(u => u.Number)
            .Select(u => new
            {
                u.Id,
                u.Number,
                u.Floor,
                Type = u.Type.ToString(),
                ActiveLease = u.Leases
                    .Where(l => l.StartDate.Date <= date && (l.EndDate == null || l.EndDate.Value.Date >= date))
                    .Select(l => new ActiveTenantDto(
                        l.Tenant.Id,
                        l.Tenant.FullName,
                        l.Tenant.Email,
                        l.Tenant.Phone,
                        l.StartDate,
                        l.EndDate,
                        l.MonthlyRent
                    ))
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        return units.Select(u => new PropertyUnitDto(u.Id, u.Number, u.Floor, u.Type, u.ActiveLease)).ToList();
    }

    public async Task<PagedResultDto<MaintenanceRequestResponseDto>> GetPagedRequestsAsync(
        RequestStatus? status,
        RequestPriority? priority,
        int? propertyId,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _db.MaintenanceRequests.AsNoTracking().AsQueryable();

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        if (priority.HasValue)
            query = query.Where(r => r.Priority == priority.Value);

        if (propertyId.HasValue)
            query = query.Where(r => r.Unit.PropertyId == propertyId.Value);

        if (from.HasValue)
            query = query.Where(r => r.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(r => r.CreatedAt <= to.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new MaintenanceRequestResponseDto(
                r.Id,
                r.UnitId,
                r.Unit.Number,
                r.Unit.PropertyId,
                r.Unit.Property.Name,
                r.TenantId,
                r.Tenant.FullName,
                r.TechnicianId,
                r.Technician != null ? r.Technician.Name : null,
                r.Category,
                r.Priority.ToString(),
                r.Status.ToString(),
                r.Description,
                r.CreatedAt,
                r.ClosedAt
            ))
            .ToListAsync(ct);

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedResultDto<MaintenanceRequestResponseDto>(items, page, pageSize, totalCount, totalPages);
    }

    public async Task<TenantHistoryDto?> GetTenantHistoryAsync(int tenantId, DateTime now, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

        if (tenant is null) return null;

        var date = now.Date;
        var leases = await _db.Leases
            .AsNoTracking()
            .Where(l => l.TenantId == tenantId)
            .OrderByDescending(l => l.StartDate)
            .Select(l => new TenantLeaseHistoryItemDto(
                l.Id,
                l.Unit.PropertyId,
                l.Unit.Property.Name,
                l.UnitId,
                l.Unit.Number,
                l.StartDate,
                l.EndDate,
                l.MonthlyRent,
                l.StartDate.Date <= date && (l.EndDate == null || l.EndDate.Value.Date >= date)
            ))
            .ToListAsync(ct);

        var requests = await _db.MaintenanceRequests
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new TenantRequestHistoryItemDto(
                r.Id,
                r.Unit.Property.Name,
                r.Unit.Number,
                r.Category,
                r.Priority.ToString(),
                r.Status.ToString(),
                r.Description,
                r.CreatedAt,
                r.ClosedAt,
                r.Visits.Count,
                r.Visits.Sum(v => (decimal?)v.Cost) ?? 0m
            ))
            .ToListAsync(ct);

        return new TenantHistoryDto(tenant.Id, tenant.FullName, tenant.Email, tenant.Phone, leases, requests);
    }

    // Reporte 1: T-SQL ejecutado con Dapper
    public async Task<IReadOnlyList<PropertySummaryReportDto>> GetPropertiesSummaryReportAsync(DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        const string sql = """
            WITH RequestMetrics AS (
                SELECT
                    u.PropertyId,
                    SUM(CASE WHEN mr.Status IN ('Abierta', 'Asignada', 'EnProgreso') THEN 1 ELSE 0 END) AS OpenRequests,
                    SUM(CASE WHEN mr.Status = 'Cerrada' THEN 1 ELSE 0 END) AS ClosedRequests,
                    AVG(
                        CASE WHEN mr.Status = 'Cerrada' AND mr.ClosedAt IS NOT NULL
                        THEN CAST(DATEDIFF(MINUTE, mr.CreatedAt, mr.ClosedAt) AS FLOAT) / 60.0
                        ELSE NULL END
                    ) AS AvgResolutionHours
                FROM dbo.Units u
                LEFT JOIN dbo.MaintenanceRequests mr
                    ON mr.UnitId = u.Id
                    AND (@From IS NULL OR mr.CreatedAt >= @From)
                    AND (@To IS NULL OR mr.CreatedAt <= @To)
                GROUP BY u.PropertyId
            ),
            VisitCosts AS (
                SELECT
                    u.PropertyId,
                    COALESCE(SUM(v.Cost), 0) AS TotalVisitsCost
                FROM dbo.Units u
                INNER JOIN dbo.MaintenanceRequests mr ON mr.UnitId = u.Id
                INNER JOIN dbo.Visits v ON v.MaintenanceRequestId = mr.Id
                WHERE (@From IS NULL OR v.VisitDate >= @From)
                  AND (@To IS NULL OR v.VisitDate <= @To)
                GROUP BY u.PropertyId
            )
            SELECT
                p.Id AS PropertyId,
                p.Name AS PropertyName,
                p.City AS City,
                COALESCE(rm.OpenRequests, 0) AS OpenRequests,
                COALESCE(rm.ClosedRequests, 0) AS ClosedRequests,
                ROUND(rm.AvgResolutionHours, 2) AS AvgResolutionHours,
                COALESCE(vc.TotalVisitsCost, 0) AS TotalVisitsCost
            FROM dbo.Properties p
            LEFT JOIN RequestMetrics rm ON rm.PropertyId = p.Id
            LEFT JOIN VisitCosts vc ON vc.PropertyId = p.Id
            ORDER BY p.Id;
            """;

        var connection = _db.Database.GetDbConnection();
        var command = new CommandDefinition(sql, new { From = from, To = to }, cancellationToken: ct);
        var rows = await connection.QueryAsync<PropertySummaryReportDto>(command);
        return rows.ToList();
    }

    // Reporte 2: T-SQL ejecutado con Dapper
    public async Task<IReadOnlyList<TechnicianWorkloadReportDto>> GetTechniciansWorkloadReportAsync(DateTime now, CancellationToken ct = default)
    {
        var oneMonthAgo = now.AddMonths(-1);

        const string sql = """
            SELECT
                t.Id AS TechnicianId,
                t.Name AS TechnicianName,
                t.Specialty AS Specialty,
                COALESCE(req.ActiveRequestsCount, 0) AS ActiveRequestsCount,
                COALESCE(vis.VisitsLastMonthCount, 0) AS VisitsLastMonthCount,
                COALESCE(vis.AccumulatedCost, 0) AS AccumulatedCost
            FROM dbo.Technicians t
            LEFT JOIN (
                SELECT
                    TechnicianId,
                    COUNT(*) AS ActiveRequestsCount
                FROM dbo.MaintenanceRequests
                WHERE Status IN ('Asignada', 'EnProgreso')
                GROUP BY TechnicianId
            ) req ON req.TechnicianId = t.Id
            LEFT JOIN (
                SELECT
                    TechnicianId,
                    COUNT(*) AS VisitsLastMonthCount,
                    COALESCE(SUM(Cost), 0) AS AccumulatedCost
                FROM dbo.Visits
                WHERE VisitDate >= @OneMonthAgo
                GROUP BY TechnicianId
            ) vis ON vis.TechnicianId = t.Id
            WHERE t.IsActive = 1
            ORDER BY ActiveRequestsCount DESC, VisitsLastMonthCount DESC, t.Name ASC;
            """;

        var connection = _db.Database.GetDbConnection();
        var command = new CommandDefinition(sql, new { OneMonthAgo = oneMonthAgo }, cancellationToken: ct);
        var rows = await connection.QueryAsync<TechnicianWorkloadReportDto>(command);
        return rows.ToList();
    }
}