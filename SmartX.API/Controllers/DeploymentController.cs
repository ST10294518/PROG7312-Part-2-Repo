using Microsoft.AspNetCore.Mvc;
using SmartX.API.Models;
using SmartX.API.Services;

namespace SmartX.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeploymentController : ControllerBase
{
    [HttpPost("validate")]
    public IActionResult ValidateDeployment([FromBody] DeploymentNode deployment)
    {
        bool isValid = DeploymentValidator.Validate(deployment);

        return Ok(new
        {
            valid = isValid,
            message = isValid
                ? "Deployment configuration is valid."
                : "Deployment configuration is invalid."
        });
    }
}