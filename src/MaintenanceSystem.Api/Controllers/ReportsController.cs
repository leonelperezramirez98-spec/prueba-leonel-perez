using MaintenanceSystem.Application.DTOs;
using MaintenanceSystem.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace MaintenanceSystem.Api.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly MaintenanceService _service;

    public ReportsController(MaintenanceService service)
    {
        _service = service;
    }

    [HttpGet("properties-summary")]
    [ProducesResponseType(typeof(IReadOnlyList<PropertySummaryReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPropertiesSummary(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var report = await _service.GetPropertiesSummaryReportAsync(from, to, ct);
        return Ok(report);
    }

    [HttpGet("technicians-workload")]
    [ProducesResponseType(typeof(IReadOnlyList<TechnicianWorkloadReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTechniciansWorkload(CancellationToken ct)
    {
        var report = await _service.GetTechniciansWorkloadReportAsync(ct);
        return Ok(report);
    }
}