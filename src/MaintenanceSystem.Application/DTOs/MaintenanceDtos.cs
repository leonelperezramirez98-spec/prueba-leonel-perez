using System.ComponentModel.DataAnnotations;
using MaintenanceSystem.Domain.Enums;

namespace MaintenanceSystem.Application.DTOs;

// Escritura
public record CreateMaintenanceRequestDto(
    [Required] int UnitId,
    [Required] int TenantId,
    [Required, MaxLength(100)] string Category,
    [Required] RequestPriority Priority,
    [Required, MaxLength(1000)] string Description
);

public record RegisterVisitDto(
    [Required] int TechnicianId,
    DateTime? VisitDate,
    [Required, MaxLength(1000)] string Notes,
    [Range(0, double.MaxValue, ErrorMessage = "El costo no puede ser negativo.")] decimal Cost
);

public record ChangeRequestStatusDto(
    [Required] RequestStatus Status,
    int? TechnicianId
);

// Respuestas
public record MaintenanceRequestResponseDto(
    int Id,
    int UnitId,
    string UnitNumber,
    int PropertyId,
    string PropertyName,
    int TenantId,
    string TenantName,
    int? TechnicianId,
    string? TechnicianName,
    string Category,
    string Priority,
    string Status,
    string Description,
    DateTime CreatedAt,
    DateTime? ClosedAt
);

public record VisitResponseDto(
    int Id,
    int MaintenanceRequestId,
    int TechnicianId,
    string TechnicianName,
    DateTime VisitDate,
    string Notes,
    decimal Cost
);

public record PagedResultDto<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
);

// Consultas y Reportes
public record PropertyUnitDto(
    int UnitId,
    string Number,
    int Floor,
    string Type,
    ActiveTenantDto? CurrentTenant
);

public record ActiveTenantDto(
    int TenantId,
    string FullName,
    string Email,
    string Phone,
    DateTime LeaseStartDate,
    DateTime? LeaseEndDate,
    decimal MonthlyRent
);

public record TenantHistoryDto(
    int TenantId,
    string FullName,
    string Email,
    string Phone,
    IReadOnlyList<TenantLeaseHistoryItemDto> Leases,
    IReadOnlyList<TenantRequestHistoryItemDto> MaintenanceRequests
);

public record TenantLeaseHistoryItemDto(
    int LeaseId,
    int PropertyId,
    string PropertyName,
    int UnitId,
    string UnitNumber,
    DateTime StartDate,
    DateTime? EndDate,
    decimal MonthlyRent,
    bool IsActive
);

public record TenantRequestHistoryItemDto(
    int RequestId,
    string PropertyName,
    string UnitNumber,
    string Category,
    string Priority,
    string Status,
    string Description,
    DateTime CreatedAt,
    DateTime? ClosedAt,
    int VisitsCount,
    decimal TotalVisitsCost
);

public record PropertySummaryReportDto(
    int PropertyId,
    string PropertyName,
    string City,
    int OpenRequests,
    int ClosedRequests,
    double? AvgResolutionHours,
    decimal TotalVisitsCost
);

public record TechnicianWorkloadReportDto(
    int TechnicianId,
    string TechnicianName,
    string Specialty,
    int ActiveRequestsCount,
    int VisitsLastMonthCount,
    decimal AccumulatedCost
);