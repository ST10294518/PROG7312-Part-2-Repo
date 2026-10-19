using Microsoft.AspNetCore.Mvc;
using SmartX.API.Models;
using SmartX.API.Services;

namespace SmartX.API.Controllers;

public sealed class ManualCommandRequest
{
    public Guid SensorId { get; set; }
    public bool? State { get; set; }
}

[ApiController]
[Route("api/[controller]")]
public class CommandController : ControllerBase
{
    private readonly CommandService _commands;

    public CommandController(CommandService commands)
    {
        _commands = commands;
    }

    [HttpGet("state/{sensorId:guid}")]
    public IActionResult GetState(Guid sensorId)
    {
        var state = _commands.GetState(sensorId);

        if (state is null)
            return NotFound(new { message = "Registered actuator not found." });

        return Ok(new ActuatorStateResponse(sensorId, state.Value));
    }

    [HttpGet("history")]
    public IActionResult GetHistory([FromQuery] int count = 50)
    {
        return Ok(_commands.GetHistory(Math.Clamp(count, 1, 500)));
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new { undoCount = _commands.UndoCount });
    }

    [HttpPost]
    public IActionResult Execute([FromBody] ManualCommandRequest request)
    {
        if (request.SensorId == Guid.Empty || request.State is null)
        {
            return BadRequest(new
            {
                message = "Sensor ID and a Boolean state are required."
            });
        }

        var result = _commands.Execute(request.SensorId, request.State.Value);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("undo")]
    public IActionResult Undo()
    {
        var result = _commands.Undo();

        if (!result.Success)
            return Conflict(result);

        return Ok(result);
    }
}
