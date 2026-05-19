using System.Net;
using FluentAssertions;
using TaskManagement.Application.DTOs;
using TaskManagement.IntegrationTests.Infrastructure;

namespace TaskManagement.IntegrationTests;

public class AuthEndpointsTests : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client;
    public AuthEndpointsTests(TestWebAppFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Register_then_login_returns_jwt()
    {
        var register = await _client.PostJson("/api/auth/register",
            new RegisterDto("bob", "bob@example.com", "Passw0rd!Strong"));
        register.StatusCode.Should().Be(HttpStatusCode.OK);

        var login = await _client.PostJson("/api/auth/login",
            new LoginDto("bob", "Passw0rd!Strong"));
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await login.ReadJson<AuthResultDto>();
        body!.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Register_with_weak_password_returns_400()
    {
        var res = await _client.PostJson("/api/auth/register",
            new RegisterDto("weak", "weak@example.com", "short"));
        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_with_invalid_credentials_returns_401()
    {
        var res = await _client.PostJson("/api/auth/login",
            new LoginDto("nobody", "Passw0rd!Strong"));
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Tasks_endpoint_without_token_returns_401()
    {
        var res = await _client.GetAsync("/api/tasks");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Security_headers_are_present_on_every_response()
    {
        var res = await _client.GetAsync("/api/tasks"); // 401 is fine; headers attach anyway
        res.Headers.TryGetValues("X-Content-Type-Options", out var nosniff).Should().BeTrue();
        nosniff!.Should().Contain("nosniff");
        res.Headers.TryGetValues("X-Frame-Options", out var frame).Should().BeTrue();
        frame!.Should().Contain("DENY");
        res.Headers.TryGetValues("Referrer-Policy", out var refp).Should().BeTrue();
        refp!.Should().Contain("no-referrer");
    }
}
