namespace TaskManagement.Application.DTOs;

public record RegisterDto(string UserName, string Email, string Password);
public record LoginDto(string UserName, string Password);
public record AuthResultDto(string Token, DateTime ExpiresAt);
