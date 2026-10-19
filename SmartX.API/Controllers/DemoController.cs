using Microsoft.AspNetCore.Mvc;
using SmartX.API.Services;

namespace SmartX.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DemoController : ControllerBase
{
    private readonly TelemetrySimulationService _simulation;

    public DemoController(TelemetrySimulationService simulation)
    {
        _simulation = simulation;
    }

    [HttpPost("seed")]
    public async Task<IActionResult> Seed(CancellationToken cancellationToken)
    {
        var accepted = await _simulation.RunCycleAsync(cancellationToken);

        return Ok(new
        {
            message = "Simulation cycle completed through the telemetry HTTP API.",
            accepted
        });
    }
}
