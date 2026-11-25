using MaintenanceSystem.Application.DTOs;
using MaintenanceSystem.Application.Interfaces;
using MaintenanceSystem.Domain.Entities;
using MaintenanceSystem.Domain.Enums;
using MaintenanceSystem.Domain.Exceptions;

namespace MaintenanceSystem.Application.Services;

public class MaintenanceService
{
    private readonly IMaintenanceRepository _repository;

    public MaintenanceService(IMaintenanceRepository repository)
    {
        _repository = repository;
    }

    public async Task<MaintenanceRequestResponseDto> CreateRequestAsync(CreateMaintenanceRequestDto dto, CancellationToken ct = default)
    {
        var unit = await _repository.GetUnitWithLeasesAsync(dto.UnitId, ct)
            ?? throw new DomainException($"La unidad con ID {dto.UnitId} no existe.", 404);

        var now = DateTime.UtcNow;
        var request = MaintenanceRequest.Create(unit, dto.TenantId, dto.Category, dto.Priority, dto.Description, now);

        await _repository.AddRequestAsync(request, ct);
        await _repository.SaveChangesWithTransactionAsync(ct);

        var created = await _repository.GetRequestWithDetailsAsync(request.Id, ct);
        return MapToResponse(created!);
    }

    public async Task<VisitResponseDto> RegisterVisitAsync(int requestId, RegisterVisitDto dto, CancellationToken ct = default)
    {
        var request = await _repository.GetRequestWithDetailsAsync(requestId, ct)
            ?? throw new DomainException($"La solicitud con ID {requestId} no existe.", 404);

        var technician = await _repository.GetTechnicianByIdAsync(dto.TechnicianId, ct)
            ?? throw new DomainException($"El técnico con ID {dto.TechnicianId} no existe.", 404);

        if (!technician.IsActive)
            throw new DomainException("No se puede registrar una visita con un técnico inactivo.", 400);

        var visitDate = dto.VisitDate ?? DateTime.UtcNow;
        var visit = request.RegisterVisit(technician, visitDate, dto.Notes, dto.Cost);

        await _repository.AddVisitAsync(visit, ct);
        await _repository.SaveChangesWithTransactionAsync(ct);

        return new VisitResponseDto(
            visit.Id,
            visit.MaintenanceRequestId,
            technician.Id,
            technician.Name,
            visit.VisitDate,
            visit.Notes,
            visit.Cost
        );
    }

    public async Task<MaintenanceRequestResponseDto> ChangeStatusAsync(int requestId, ChangeRequestStatusDto dto, CancellationToken ct = default)
    {
        var request = await _repository.GetRequestWithDetailsAsync(requestId, ct)
            ?? throw new DomainException($"La solicitud con ID {requestId} no existe.", 404);

        Technician? technician = null;
        if (dto.TechnicianId.HasValue)
        {
            technician = await _repository.GetTechnicianByIdAsync(dto.TechnicianId.Value, ct)
                ?? throw new DomainException($"El técnico con ID {dto.TechnicianId.Value} no existe.", 404);
        }

        request.ChangeStatus(dto.Status, technician, DateTime.UtcNow);
        await _repository.SaveChangesWithTransactionAsync(ct);

        return MapToResponse(request);
    }

    public async Task<IReadOnlyList<PropertyUnitDto>> GetUnitsByPropertyAsync(int propertyId, CancellationToken ct = default)
    {
        var units = await _repository.GetUnitsByPropertyAsync(propertyId, DateTime.UtcNow, ct)
            ?? throw new DomainException($"El edificio con ID {propertyId} no existe.", 404);
        return units;
    }

    public Task<PagedResultDto<MaintenanceRequestResponseDto>> GetPagedRequestsAsync(
        RequestStatus? status,
        RequestPriority? priority,
        int? propertyId,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 10 : pageSize;
        return _repository.GetPagedRequestsAsync(status, priority, propertyId, from, to, page, pageSize, ct);
    }

    public async Task<TenantHistoryDto> GetTenantHistoryAsync(int tenantId, CancellationToken ct = default)
    {
        return await _repository.GetTenantHistoryAsync(tenantId, DateTime.UtcNow, ct)
            ?? throw new DomainException($"El inquilino con ID {tenantId} no existe.", 404);
    }

    public Task<IReadOnlyList<PropertySummaryReportDto>> GetPropertiesSummaryReportAsync(DateTime? from, DateTime? to, CancellationToken ct = default)
        => _repository.GetPropertiesSummaryReportAsync(from, to, ct);

    public Task<IReadOnlyList<TechnicianWorkloadReportDto>> GetTechniciansWorkloadReportAsync(CancellationToken ct = default)
        => _repository.GetTechniciansWorkloadReportAsync(DateTime.UtcNow, ct);

    private static MaintenanceRequestResponseDto MapToResponse(MaintenanceRequest r) => new(
        r.Id,
        r.UnitId,
        r.Unit.Number,
        r.Unit.PropertyId,
        r.Unit.Property.Name,
        r.TenantId,
        r.Tenant.FullName,
        r.TechnicianId,
        r.Technician?.Name,
        r.Category,
        r.Priority.ToString(),
        r.Status.ToString(),
        r.Description,
        r.CreatedAt,
        r.ClosedAt
    );
}