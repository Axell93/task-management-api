using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.API.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController(IIdentityService identity, ILogger<AuthController> logger) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResultDto>> Register([FromBody] RegisterDto dto, CancellationToken ct)
    {
        logger.LogInformation("POST /api/auth/register (userName={UserName})", dto.UserName);
        return Ok(await identity.RegisterAsync(dto, ct));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResultDto>> Login([FromBody] LoginDto dto, CancellationToken ct)
    {
        logger.LogInformation("POST /api/auth/login (userName={UserName})", dto.UserName);
        return Ok(await identity.LoginAsync(dto, ct));
    }
}
