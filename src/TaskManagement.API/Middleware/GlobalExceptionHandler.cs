using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.Common;

namespace TaskManagement.API.Middleware;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        var (status, problem) = exception switch
        {
            UnauthorizedException ue => (
                StatusCodes.Status401Unauthorized,
                (ProblemDetails)new ProblemDetails
                {
                    Title = "Unauthorized",
                    Status = StatusCodes.Status401Unauthorized,
                    Detail = ue.Message
                }),
            LockedOutException le => (
                StatusCodes.Status423Locked,
                (ProblemDetails)new ProblemDetails
                {
                    Title = "Locked",
                    Status = StatusCodes.Status423Locked,
                    Detail = le.Message
                }),
            NotFoundException nf => (
                StatusCodes.Status404NotFound,
                (ProblemDetails)new ProblemDetails
                {
                    Title = "Not Found",
                    Status = StatusCodes.Status404NotFound,
                    Detail = nf.Message
                }),
            ConflictException cf => (
                StatusCodes.Status409Conflict,
                (ProblemDetails)new ProblemDetails
                {
                    Title = "Conflict",
                    Status = StatusCodes.Status409Conflict,
                    Detail = cf.Message
                }),
            Application.Common.ValidationException ve => (
                StatusCodes.Status400BadRequest,
                (ProblemDetails)new ValidationProblemDetails(ve.Errors)
                {
                    Title = "Validation failed",
                    Status = StatusCodes.Status400BadRequest
                }),
            _ => (
                StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Title = "Internal Server Error",
                    Status = StatusCodes.Status500InternalServerError,
                    Detail = "An unexpected error occurred."
                })
        };

        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = "application/problem+json";
        // Serialize against the runtime type so ValidationProblemDetails.Errors
        // is included rather than truncated to the ProblemDetails base.
        await httpContext.Response.WriteAsJsonAsync(problem, problem.GetType(), cancellationToken);
        return true;
    }
}
