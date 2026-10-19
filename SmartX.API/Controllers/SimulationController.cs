using Microsoft.AspNetCore.Mvc;
using SmartX.API.Services;

namespace SmartX.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SimulationController : ControllerBase
{
    private readonly TelemetrySimulationService _simulation;
    private readonly SensorService _sensors;

    public SimulationController(
        TelemetrySimulationService simulation,
        SensorService sensors)
    {
        _simulation = simulation;
        _sensors = sensors;
    }

    [HttpGet("paused")]
    public async Task<IActionResult> GetPaused(
        CancellationToken cancellationToken)
    {
        return Ok(await _simulation.GetPausedAsync(cancellationToken));
    }

    [HttpPost("{sensorId:guid}/pause")]
    public Task<IActionResult> Pause(
        Guid sensorId,
        CancellationToken cancellationToken)
    {
        return SetPaused(sensorId, true, cancellationToken);
    }

    [HttpPost("{sensorId:guid}/resume")]
    public Task<IActionResult> Resume(
        Guid sensorId,
        CancellationToken cancellationToken)
    {
        return SetPaused(sensorId, false, cancellationToken);
    }

    private async Task<IActionResult> SetPaused(
        Guid sensorId,
        bool paused,
        CancellationToken cancellationToken)
    {
        if (_sensors.GetById(sensorId) is null)
            return NotFound(new { message = "Sensor not found." });

        await _simulation.SetPausedAsync(
            sensorId, paused, cancellationToken);

        return Ok(new
        {
            sensorId,
            paused,
            message = paused
                ? "Device simulation paused."
                : "Device simulation resumed."
        });
    }
}
