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
            version = "Part 1",
            activeModule = "Sensor Data Ingestion and Telemetry",
            apiTime = DateTime.UtcNow
        });
    }
}