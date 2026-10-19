using Microsoft.AspNetCore.Mvc;
using SmartX.API.Models;
using SmartX.API.Services;

namespace SmartX.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TelemetryController : ControllerBase
{
    private readonly TelemetryService _telemetryService;

    public TelemetryController(
        TelemetryService telemetryService)
    {
        _telemetryService = telemetryService;
    }

    [HttpGet]
    public IActionResult GetLatest(
        [FromQuery] int count = 50)
    {
        count = Math.Clamp(count, 1, 500);

        return Ok(
            _telemetryService.GetLatest(count));
    }

    [HttpGet("statistics")]
    public IActionResult GetStatistics()
    {
        return Ok(
            _telemetryService.GetStatistics());
    }

    [HttpGet("sensor/{sensorId}")]
    public IActionResult GetSensorHistory(
        string sensorId,
        [FromQuery] int count = 50)
    {
        return Ok(
            _telemetryService.GetSensorHistory(
                sensorId,
                count));
    }

    [HttpPost]
    public IActionResult PostTelemetry(
        [FromBody] TelemetryInput input)
    {
        var result =
            _telemetryService.Process(input);

        if (!result.Success)
            return BadRequest(new
            {
                message = result.Message
            });

        return Ok(new
        {
            message = result.Message,
            record = result.Record
        });
    }
}