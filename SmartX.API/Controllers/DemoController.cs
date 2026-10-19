using Microsoft.AspNetCore.Mvc;
using SmartX.API.Models;
using SmartX.API.Services;

namespace SmartX.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DemoController : ControllerBase
{
    private readonly SensorService _sensorService;
    private readonly TelemetryService _telemetryService;

    public DemoController(
        SensorService sensorService,
        TelemetryService telemetryService)
    {
        _sensorService = sensorService;
        _telemetryService = telemetryService;
    }

    [HttpPost("seed")]
    public IActionResult Seed()
    {
        var sensors = _sensorService.GetAll();

        var random = new Random();

        foreach (var sensor in sensors)
        {
            if (sensor.SensorCategory == "Environmental")
            {
                Send(
                    sensor,
                    "Temperature",
                    "float",
                    random.NextDouble() * 45);

                Send(
                    sensor,
                    "Moisture",
                    "float",
                    random.NextDouble() * 100);
            }
            else if (sensor.SensorCategory == "Power Consumption")
            {
                Send(
                    sensor,
                    "Power",
                    "int",
                    random.Next(500, 5000));
            }
            else
            {
                Send(
                    sensor,
                    "ValveState",
                    "bool",
                    random.Next(0, 2) == 1);
            }
        }

        return Ok(new
        {
            message = "Demo telemetry generated successfully.",
            readings = _telemetryService.GetLatest(10)
        });
    }

    private void Send(
        Sensor sensor,
        string metric,
        string type,
        object value)
    {
        var json =
            System.Text.Json.JsonSerializer.SerializeToElement(
                value);

        _telemetryService.Process(
            new TelemetryInput
            {
                SensorId = sensor.Id.ToString(),
                Metric = metric,
                ValueType = type,
                Value = json
            });
    }
}