using Microsoft.AspNetCore.Mvc;
using SmartX.API.Services;

namespace SmartX.API.Controllers;

public sealed class DeviceSearchRequest
{
    public Guid SensorId { get; set; }
    public string? Query { get; set; }
}

[ApiController]
[Route("api/[controller]")]
public class RecommendationController : ControllerBase
{
    private readonly ActionLearningService _learning;
    private readonly RecommendationService _recommendations;
    private readonly ActiveAlertService _alerts;
    private readonly SensorService _sensors;

    public RecommendationController(
        ActionLearningService learning,
        RecommendationService recommendations,
        ActiveAlertService alerts,
        SensorService sensors)
    {
        _learning = learning;
        _recommendations = recommendations;
        _alerts = alerts;
        _sensors = sensors;
    }

    [HttpGet]
    public IActionResult GetSuggestions()
    {
        return Ok(_recommendations.GetSuggestions());
    }

    [HttpGet("patterns")]
    public IActionResult GetPatterns()
    {
        return Ok(_learning.GetPatterns());
    }

    [HttpGet("searches")]
    public IActionResult GetSearchHistory([FromQuery] int count = 50)
    {
        return Ok(_learning.GetSearchHistory(Math.Clamp(count, 1, 200)));
    }

    [HttpPost("searches")]
    public IActionResult RecordSearch([FromBody] DeviceSearchRequest request)
    {
        if (request.SensorId == Guid.Empty ||
            string.IsNullOrWhiteSpace(request.Query) ||
            request.Query.Trim().Length > 100)
        {
            return BadRequest(new
            {
                message = "Select a device and supply search text of 1-100 characters."
            });
        }

        if (_sensors.GetById(request.SensorId) is null)
            return NotFound(new { message = "Sensor not found." });

        _learning.RecordSearch(
            request.SensorId,
            request.Query,
            _alerts.GetActive(request.SensorId));

        return Ok(new { message = "Device search recorded." });
    }
}
