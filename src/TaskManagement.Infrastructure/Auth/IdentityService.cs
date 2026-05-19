using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TaskManagement.Application.Common;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;
using TaskManagement.Infrastructure.Identity;

namespace TaskManagement.Infrastructure.Auth;

public class IdentityService(
    UserManager<ApplicationUser> userManager,
    IOptions<JwtOptions> jwtOptions,
    ILogger<IdentityService> logger) : IIdentityService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<AuthResultDto> RegisterAsync(RegisterDto dto, CancellationToken ct)
    {
        logger.LogInformation("Registering user {UserName}", dto.UserName);
        var user = new ApplicationUser { UserName = dto.UserName, Email = dto.Email };
        var result = await userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors
                .GroupBy(e => e.Code)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
            logger.LogInformation("Registration failed for {UserName}: {Errors}", dto.UserName, string.Join("; ", errors.Keys));
            throw new Application.Common.ValidationException(errors);
        }
        logger.LogInformation("User {UserName} registered (id={UserId})", user.UserName, user.Id);
        return BuildToken(user);
    }

    public async Task<AuthResultDto> LoginAsync(LoginDto dto, CancellationToken ct)
    {
        logger.LogInformation("Login attempt for {UserName}", dto.UserName);
        var user = await userManager.FindByNameAsync(dto.UserName);

        // Always return the same generic error so the response cannot be used
        // to enumerate which usernames exist.
        if (user is null)
        {
            logger.LogInformation("Login failed for {UserName} (no such user)", dto.UserName);
            throw new UnauthorizedException();
        }

        // Honour Identity's lockout state if the account is already locked.
        if (await userManager.IsLockedOutAsync(user))
        {
            logger.LogWarning("Login blocked for {UserName} — account locked until {Until:o}",
                dto.UserName, await userManager.GetLockoutEndDateAsync(user));
            throw new LockedOutException();
        }

        var ok = await userManager.CheckPasswordAsync(user, dto.Password);
        if (!ok)
        {
            // Increment the failed-attempt counter; once it hits the threshold
            // configured in IdentityOptions.Lockout, Identity locks the account.
            await userManager.AccessFailedAsync(user);
            if (await userManager.IsLockedOutAsync(user))
            {
                logger.LogWarning("Account {UserName} just locked after repeated failures", dto.UserName);
                throw new LockedOutException();
            }
            logger.LogInformation("Login failed for {UserName} (bad password)", dto.UserName);
            throw new UnauthorizedException();
        }

        // Successful sign-in — reset the failure counter so a future bad
        // password doesn't start from a poisoned baseline.
        await userManager.ResetAccessFailedCountAsync(user);
        logger.LogInformation("Login succeeded for {UserName}", dto.UserName);
        return BuildToken(user);
    }

    private AuthResultDto BuildToken(ApplicationUser user)
    {
        var expires = DateTime.UtcNow.AddMinutes(_jwt.ExpiryMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_jwt.Issuer, _jwt.Audience, claims, expires: expires, signingCredentials: creds);
        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return new AuthResultDto(jwt, expires);
    }
}
