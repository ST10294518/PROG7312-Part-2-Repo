using Microsoft.AspNetCore.Mvc;

namespace SmartX.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SystemController : ControllerBase
{
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            name = "Smart-X Gateway",
            status = "Online",
            version = "Part 2",
            activeModule = "Sensor Data Ingestion and Telemetry",
            activeModules = new[]
            {
                "Sensor Data Ingestion and Telemetry",
                "Real-Time Command Stream and History"
            },
            apiTime = DateTime.UtcNow
        });
    }
}
