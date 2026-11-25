using MaintenanceSystem.Application.DTOs;
using MaintenanceSystem.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace MaintenanceSystem.Api.Controllers;

[ApiController]
[Route("api/properties")]
public class PropertiesController : ControllerBase
{
    private readonly MaintenanceService _service;

    public PropertiesController(MaintenanceService service)
    {
        _service = service;
    }

    [HttpGet("{id:int}/units")]
    [ProducesResponseType(typeof(IReadOnlyList<PropertyUnitDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUnits(int id, CancellationToken ct)
    {
        var units = await _service.GetUnitsByPropertyAsync(id, ct);
        return Ok(units);
    }
}