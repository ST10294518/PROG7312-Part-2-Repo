using Microsoft.AspNetCore.Mvc;
using SmartX.API.Services;

namespace SmartX.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileController : ControllerBase
{
    private readonly FileService _fileService;

    public FileController(FileService fileService)
    {
        _fileService = fileService;
    }

    [HttpPost("sensor/{sensorId:guid}")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Upload(
        Guid sensorId,
        IFormFile file)
    {
        var result =
            await _fileService.SaveEncryptedAsync(
                sensorId,
                file);

        if (!result.Success)
            return BadRequest(new
            {
                message = result.Message
            });

        return Ok(new
        {
            message = result.Message,
            fileName = result.FileName,
            sensorId
        });
    }
}