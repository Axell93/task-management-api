using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using TaskManagement.API.Middleware;
using TaskManagement.Application.Common;

namespace TaskManagement.UnitTests.Middleware;

public class GlobalExceptionHandlerTests
{
    private static HttpContext NewContext()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();
        return ctx;
    }

    private static async Task<(int Status, string Body)> Handle(Exception ex)
    {
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var ctx = NewContext();
        var handled = await handler.TryHandleAsync(ctx, ex, default);
        handled.Should().BeTrue();
        ctx.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(ctx.Response.Body, Encoding.UTF8).ReadToEndAsync();
        return (ctx.Response.StatusCode, body);
    }

    [Fact]
    public async Task NotFoundException_maps_to_404_problem_details()
    {
        var (status, body) = await Handle(new NotFoundException("missing"));
        status.Should().Be(StatusCodes.Status404NotFound);
        body.Should().Contain("\"status\":404").And.Contain("missing");
    }

    [Fact]
    public async Task UnauthorizedException_maps_to_401_with_neutral_message()
    {
        var (status, body) = await Handle(new UnauthorizedException());
        status.Should().Be(StatusCodes.Status401Unauthorized);
        body.Should().Contain("Invalid credentials");
    }

    [Fact]
    public async Task LockedOutException_maps_to_423()
    {
        var (status, _) = await Handle(new LockedOutException());
        status.Should().Be(StatusCodes.Status423Locked);
    }

    [Fact]
    public async Task ConflictException_maps_to_409()
    {
        var (status, body) = await Handle(new ConflictException("dup"));
        status.Should().Be(StatusCodes.Status409Conflict);
        body.Should().Contain("\"status\":409");
    }

    [Fact]
    public async Task ValidationException_maps_to_400_with_errors_dict()
    {
        var ex = new Application.Common.ValidationException(
            new Dictionary<string, string[]> { ["Title"] = new[] { "Required." } });

        var (status, body) = await Handle(ex);

        status.Should().Be(StatusCodes.Status400BadRequest);
        body.Should().Contain("\"Title\"").And.Contain("Required.");
    }

    [Fact]
    public async Task Unknown_exception_maps_to_500()
    {
        var (status, body) = await Handle(new InvalidOperationException("boom"));
        status.Should().Be(StatusCodes.Status500InternalServerError);
        body.Should().Contain("\"status\":500");
    }
}
