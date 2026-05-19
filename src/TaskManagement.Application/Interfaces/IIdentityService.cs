using TaskManagement.Application.DTOs;

namespace TaskManagement.Application.Interfaces;

public interface IIdentityService
{
    Task<AuthResultDto> RegisterAsync(RegisterDto dto, CancellationToken ct);
    Task<AuthResultDto> LoginAsync(LoginDto dto, CancellationToken ct);
}
