using System.Net;
using System.Text.Json;
using FluentAssertions;
using TaskManagement.IntegrationTests.Infrastructure;

namespace TaskManagement.IntegrationTests;

public class HealthEndpointsTests : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client;
    public HealthEndpointsTests(TestWebAppFactory f) => _client = f.CreateClient();

    [Fact]
    public async Task Liveness_endpoint_is_anonymous_and_reports_self_check()
    {
        var res = await _client.GetAsync("/health/live");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("status").GetString().Should().Be("Healthy");
        doc.RootElement.GetProperty("results").EnumerateArray()
            .Should().Contain(e => e.GetProperty("name").GetString() == "self");
    }

    [Fact]
    public async Task Readiness_endpoint_reports_db_check()
    {
        var res = await _client.GetAsync("/health/ready");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("status").GetString().Should().Be("Healthy");
        doc.RootElement.GetProperty("results").EnumerateArray()
            .Should().Contain(e => e.GetProperty("name").GetString() == "db");
    }

    [Fact]
    public async Task Liveness_does_NOT_include_db_check()
    {
        var res = await _client.GetAsync("/health/live");
        var body = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.GetProperty("results").EnumerateArray()
            .Should().NotContain(e => e.GetProperty("name").GetString() == "db");
    }
}
