namespace TaskManagement.API.Middleware;

/// <summary>
/// Adds the headers every API response should carry. Tighten/relax
/// individual headers via the constants below — they are not configurable
/// on purpose, so the policy is auditable in one place.
/// </summary>
public class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext ctx)
    {
        var h = ctx.Response.Headers;

        // MIME-sniffing — block browsers from guessing content types.
        h["X-Content-Type-Options"] = "nosniff";

        // Clickjacking — API responses should never be framed.
        h["X-Frame-Options"] = "DENY";

        // Don't leak the API URL via the Referer header on outbound nav.
        h["Referrer-Policy"] = "no-referrer";

        // Lock down ambient browser features for any HTML the API ever returns.
        h["Permissions-Policy"] = "geolocation=(), camera=(), microphone=()";

        // Minimal CSP — this is a JSON API, no resources should ever load.
        h["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";

        // Older IE protections; cheap to keep.
        h["X-XSS-Protection"] = "0";

        // Hide the server fingerprint.
        h.Remove("Server");

        return next(ctx);
    }
}
