using MaintenanceSystem.Application.DTOs;
using MaintenanceSystem.Application.Services;
using MaintenanceSystem.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace MaintenanceSystem.Api.Controllers;

[ApiController]
[Route("api/maintenance-requests")]
public class MaintenanceRequestsController : ControllerBase
{
    private readonly MaintenanceService _service;

    public MaintenanceRequestsController(MaintenanceService service)
    {
        _service = service;
    }

    [HttpPost]
    [ProducesResponseType(typeof(MaintenanceRequestResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateMaintenanceRequestDto dto, CancellationToken ct)
    {
        var created = await _service.CreateRequestAsync(dto, ct);
        return Created($"/api/maintenance-requests/{created.Id}", created);
    }

    [HttpPost("{id:int}/visits")]
    [ProducesResponseType(typeof(VisitResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterVisit(int id, [FromBody] RegisterVisitDto dto, CancellationToken ct)
    {
        var visit = await _service.RegisterVisitAsync(id, dto, ct);
        return Created($"/api/maintenance-requests/{id}/visits/{visit.Id}", visit);
    }

    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(typeof(MaintenanceRequestResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeStatus(int id, [FromBody] ChangeRequestStatusDto dto, CancellationToken ct)
    {
        var updated = await _service.ChangeStatusAsync(id, dto, ct);
        return Ok(updated);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<MaintenanceRequestResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged(
        [FromQuery] RequestStatus? status,
        [FromQuery] RequestPriority? priority,
        [FromQuery] int? propertyId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await _service.GetPagedRequestsAsync(status, priority, propertyId, from, to, page, pageSize, ct);
        return Ok(result);
    }
}