using MaintenanceSystem.Application.DTOs;
using MaintenanceSystem.Domain.Entities;
using MaintenanceSystem.Domain.Enums;

namespace MaintenanceSystem.Application.Interfaces;

public interface IMaintenanceRepository
{
    Task<Unit?> GetUnitWithLeasesAsync(int unitId, CancellationToken ct = default);
    Task<Technician?> GetTechnicianByIdAsync(int technicianId, CancellationToken ct = default);
    Task<MaintenanceRequest?> GetRequestWithDetailsAsync(int id, CancellationToken ct = default);
    Task AddRequestAsync(MaintenanceRequest request, CancellationToken ct = default);
    Task AddVisitAsync(Visit visit, CancellationToken ct = default);
    Task SaveChangesWithTransactionAsync(CancellationToken ct = default);

    // Consultas
    Task<IReadOnlyList<PropertyUnitDto>?> GetUnitsByPropertyAsync(int propertyId, DateTime now, CancellationToken ct = default);
    Task<PagedResultDto<MaintenanceRequestResponseDto>> GetPagedRequestsAsync(
        RequestStatus? status,
        RequestPriority? priority,
        int? propertyId,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken ct = default);
    Task<TenantHistoryDto?> GetTenantHistoryAsync(int tenantId, DateTime now, CancellationToken ct = default);
    Task<IReadOnlyList<PropertySummaryReportDto>> GetPropertiesSummaryReportAsync(DateTime? from, DateTime? to, CancellationToken ct = default);
    Task<IReadOnlyList<TechnicianWorkloadReportDto>> GetTechniciansWorkloadReportAsync(DateTime now, CancellationToken ct = default);
}