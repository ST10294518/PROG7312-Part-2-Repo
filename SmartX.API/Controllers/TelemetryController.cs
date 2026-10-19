using Microsoft.AspNetCore.Mvc;
using SmartX.API.DataStructures;
using SmartX.API.Models;
using SmartX.API.Services;

namespace SmartX.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TelemetryController : ControllerBase
{
    private readonly TelemetryService _telemetryService;
    private readonly SensorService _sensors;
    private readonly TelemetryInputValidator _validator;
    private readonly TelemetryProcessingQueue _queue;
    private readonly TelemetryQueueProcessor _processor;

    public TelemetryController(
        TelemetryService telemetryService,
        SensorService sensors,
        TelemetryInputValidator validator,
        TelemetryProcessingQueue queue,
        TelemetryQueueProcessor processor)
    {
        _telemetryService = telemetryService;
        _sensors = sensors;
        _validator = validator;
        _queue = queue;
        _processor = processor;
    }

    [HttpGet]
    public IActionResult GetLatest([FromQuery] int count = 50)
    {
        return Ok(_telemetryService.GetLatest(Math.Clamp(count, 1, 500)));
    }

    [HttpGet("statistics")]
    public IActionResult GetStatistics()
    {
        return Ok(_telemetryService.GetStatistics());
    }

    [HttpGet("sensor/{sensorId}")]
    public IActionResult GetSensorHistory(
        string sensorId,
        [FromQuery] int count = 50)
    {
        if (!Guid.TryParse(sensorId, out var id))
            return BadRequest(new { message = "Invalid sensor ID." });

        return Ok(_telemetryService.GetSensorHistory(
            id.ToString(), Math.Clamp(count, 1, 500)));
    }

    [HttpGet("queue")]
    public IActionResult GetQueueStatus()
    {
        return Ok(_processor.GetStatus());
    }

    [HttpPost]
    public IActionResult PostTelemetry([FromBody] TelemetryInput input)
    {
        var validation = _validator.Validate(input);

        if (!validation.Success)
            return BadRequest(new { message = validation.Message });

        var normalised = new TelemetryInput
        {
            SensorId = Guid.Parse(input.SensorId).ToString(),
            Metric = input.Metric.Trim(),
            ValueType = input.ValueType.ToLowerInvariant(),
            Value = input.Value
        };

        if (!_queue.TryEnqueue(
            normalised, validation.Priority, out var queued))
        {
            Response.Headers["Retry-After"] = "1";

            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = "Telemetry queue is full. Retry shortly."
            });
        }

        _sensors.MarkReceived(Guid.Parse(normalised.SensorId), queued!.EnqueuedAt);

        return Accepted(new
        {
            message = "Telemetry accepted for background processing.",
            packetId = queued!.Id,
            priority = queued.Priority,
            enqueuedAt = queued.EnqueuedAt
        });
    }
}

