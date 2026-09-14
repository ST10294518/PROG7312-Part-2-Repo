using Microsoft.AspNetCore.Mvc;
using SmartX.API.Models;
using SmartX.API.Services;

namespace SmartX.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SensorController : ControllerBase
{
    private readonly SensorService _sensorService;

    public SensorController(SensorService sensorService)
    {
        _sensorService = sensorService;
    }

    [HttpGet]
    public IActionResult GetSensors()
    {
        return Ok(_sensorService.GetAll());
    }

    [HttpGet("{id:guid}")]
    public IActionResult GetSensor(Guid id)
    {
        var sensor = _sensorService.GetById(id);

        if (sensor == null)
            return NotFound(new
            {
                message = "Sensor not found."
            });

        return Ok(sensor);
    }

    [HttpPost]
    public IActionResult RegisterSensor([FromBody] Sensor sensor)
    {
        var result =
            _sensorService.Register(
                sensor.MacAddress,
                sensor.DeploymentLocation,
                sensor.SensorCategory);

        if (!result.Success)
            return BadRequest(new
            {
                message = result.Message
            });

        return Created(
            $"/api/sensor/{result.Sensor!.Id}",
            result.Sensor);
    }
}