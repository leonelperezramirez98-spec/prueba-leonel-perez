using MaintenanceSystem.Application.DTOs;
using MaintenanceSystem.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace MaintenanceSystem.Api.Controllers;

[ApiController]
[Route("api/tenants")]
public class TenantsController : ControllerBase
{
    private readonly MaintenanceService _service;

    public TenantsController(MaintenanceService service)
    {
        _service = service;
    }

    [HttpGet("{id:int}/history")]
    [ProducesResponseType(typeof(TenantHistoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHistory(int id, CancellationToken ct)
    {
        var history = await _service.GetTenantHistoryAsync(id, ct);
        return Ok(history);
    }
}