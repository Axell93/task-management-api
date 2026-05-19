namespace TaskManagement.Application.Common;

public class NotFoundException(string message) : Exception(message);

public class ValidationException(IDictionary<string, string[]> errors)
    : Exception("One or more validation failures occurred.")
{
    public IDictionary<string, string[]> Errors { get; } = errors;
}

public class ConflictException(string message) : Exception(message);

// 401 — bad credentials, expired/invalid token at the app layer.
// Message is always neutral ("Invalid credentials.") to avoid revealing
// whether the username exists.
public class UnauthorizedException(string message = "Invalid credentials.") : Exception(message);

// 423 (or 429-style) — account temporarily locked due to repeated failures.
public class LockedOutException(string message = "Account temporarily locked. Try again later.") : Exception(message);
