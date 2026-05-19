using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TaskManagement.API.HealthChecks;

internal static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    /// <summary>
    /// Renders a HealthReport as compact JSON. Each entry exposes name,
    /// status, duration and (optionally) description / exception message —
    /// safe for downstream consumers like Kubernetes probes or uptime
    /// monitors but free of stack traces.
    /// </summary>
    public static Task Write(HttpContext ctx, HealthReport report)
    {
        ctx.Response.ContentType = "application/json; charset=utf-8";

        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            results = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                durationMs = e.Value.Duration.TotalMilliseconds,
                description = e.Value.Description,
                error = e.Value.Exception?.Message,
                tags = e.Value.Tags,
            }),
        };

        return ctx.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOpts));
    }
}
