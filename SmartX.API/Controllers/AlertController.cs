using Microsoft.AspNetCore.Mvc;
using SmartX.API.Services;

namespace SmartX.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AlertController : ControllerBase
{
    private readonly ActiveAlertService _alerts;

    public AlertController(ActiveAlertService alerts)
    {
        _alerts = alerts;
    }

    [HttpGet]
    public IActionResult GetActive([FromQuery] Guid? sensorId = null)
    {
        return Ok(_alerts.GetActive(sensorId));
    }

    [HttpGet("count")]
    public IActionResult GetCount()
    {
        return Ok(new { activeAlerts = _alerts.Count });
    }
}
