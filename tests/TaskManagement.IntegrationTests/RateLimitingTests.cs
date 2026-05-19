using System.Net;
using Microsoft.AspNetCore.Hosting;
using TaskManagement.Application.DTOs;
using TaskManagement.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace TaskManagement.IntegrationTests;

/// <summary>
/// Re-uses the integration test factory but tightens the auth rate limit
/// to 2 requests/min so a few sequential calls trip the 429 path.
/// </summary>
public class RateLimitingTests : IClassFixture<RateLimitingTests.TightFactory>
{
    private readonly HttpClient _client;
    public RateLimitingTests(TightFactory f) => _client = f.CreateClient();

    [Fact]
    public async Task Auth_endpoint_returns_429_after_burst()
    {
        // The factory permits 2 calls/min/IP. A third must be rejected.
        var responses = new List<HttpResponseMessage>();
        for (var i = 0; i < 4; i++)
        {
            responses.Add(await _client.PostJson("/api/auth/login",
                new LoginDto($"u{i}", "Passw0rd!Strong")));
        }
        responses.Should().Contain(r => r.StatusCode == HttpStatusCode.TooManyRequests);
    }

    public class TightFactory : TestWebAppFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseSetting("RateLimit:Auth:PermitLimit", "2");
        }
    }
}
