using System.Threading.RateLimiting;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TaskManagement.API.HealthChecks;
using TaskManagement.API.Middleware;
using TaskManagement.Application;
using TaskManagement.Infrastructure;
using TaskManagement.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Feature flag: when FeatureFlags:InfoLoggingEnabled is false, drop every
// logger under the TaskManagement.* namespace to Warning+ so info logs vanish
// without code changes. Framework logs remain governed by Logging:LogLevel.
var infoLoggingEnabled = builder.Configuration.GetValue("FeatureFlags:InfoLoggingEnabled", true);
if (!infoLoggingEnabled)
{
    builder.Logging.AddFilter("TaskManagement", LogLevel.Warning);
}

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddFluentValidationAutoValidation();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Health checks:
//   - "self" (tag: live)  — process is up and the pipeline can answer.
//                            Used by k8s livenessProbe; restart on failure.
//   - "db"   (tag: ready) — AppDbContext can reach SQL Server.
//                            Used by k8s readinessProbe; removed from LB on failure.
builder.Services
    .AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("API process responsive"), tags: ["live"])
    .AddDbContextCheck<AppDbContext>(
        name: "db",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready"]);

// In-memory cache — used inside the repository for the raw-SQL summary query.
builder.Services.AddMemoryCache();

// Response-level Output Cache (built into ASP.NET Core 9). Two named
// policies tagged so the service layer can evict them on writes.
//   - TasksList:    varies by query string (status/priority) + Authorization
//   - TasksSummary: short TTL, no variance (one shape per DB state)
// Concurrency note: OutputCache uses an in-process store by default; behind
// a load balancer swap to a distributed store (Redis) via AddStackExchangeRedisOutputCache.
builder.Services.AddOutputCache(opt =>
{
    opt.AddPolicy("TasksList", b => b
        .Tag("tasks")
        .SetVaryByQuery("status", "priority")
        .SetVaryByHeader("Authorization")
        .Expire(TimeSpan.FromSeconds(30)));

    opt.AddPolicy("TasksSummary", b => b
        .Tag("tasks", "summary")
        .Expire(TimeSpan.FromSeconds(60)));
});

builder.Services.AddEndpointsApiExplorer(); 
builder.Services.AddSwaggerGen(c => { 
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TaskManagement API", Version = "v1" }); 
    var bearer = new OpenApiSecurityScheme { 
        Name = "Authorization", 
        Type = SecuritySchemeType.Http, 
        Scheme = "bearer", 
        BearerFormat = "JWT", 
        In = ParameterLocation.Header, 
        Reference = new OpenApiReference { 
            Id = "Bearer", 
            Type = ReferenceType.SecurityScheme 
        } 
    }; 
    c.AddSecurityDefinition("Bearer", bearer); 
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { 
        [bearer] = Array.Empty<string>() }); 
});


// CORS — explicit origin list, only the headers/methods the SPA actually
// uses. Add production origins via configuration when deploying.
builder.Services.AddCors(opt =>
    opt.AddPolicy("WebClient", p => p
        .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
        .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
        .WithHeaders("Authorization", "Content-Type", "Idempotency-Key")
        .WithExposedHeaders("Idempotency-Key")));

// Rate limiting:
//   - "auth"  : N calls/min per remote IP — brute-force protection.
//   - "tasks" : M calls/min per authenticated user (fallback: IP) — soft cap.
// Both reject extras with HTTP 429. Limits are configurable so test/load
// environments can relax them without code changes.
var authPermits = builder.Configuration.GetValue("RateLimit:Auth:PermitLimit", 5);
var tasksPermits = builder.Configuration.GetValue("RateLimit:Tasks:PermitLimit", 100);

builder.Services.AddRateLimiter(opt =>
{
    opt.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    opt.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = authPermits,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true,
        }));

    opt.AddPolicy("tasks", ctx => RateLimitPartition.GetSlidingWindowLimiter(
        partitionKey: ctx.User?.Identity?.Name
                      ?? ctx.Connection.RemoteIpAddress?.ToString()
                      ?? "anon",
        factory: _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = tasksPermits,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 6,
            QueueLimit = 0,
            AutoReplenishment = true,
        }));
});

// Honour X-Forwarded-* headers so RemoteIpAddress reflects the real client
// when running behind a reverse proxy / load balancer.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

// Enforce HSTS for at least 60 days in production.
builder.Services.AddHsts(opt =>
{
    opt.MaxAge = TimeSpan.FromDays(60);
    opt.IncludeSubDomains = true;
    opt.Preload = false;
});

var app = builder.Build();

app.Logger.LogInformation(
    "TaskManagement.API starting (Environment={Env}, InfoLoggingEnabled={Flag})",
    app.Environment.EnvironmentName, infoLoggingEnabled);

// ForwardedHeaders must run before anything that reads RemoteIpAddress
// (rate limiter, logging), so it goes first.
app.UseForwardedHeaders();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskManagement API v1");
        c.RoutePrefix = "swagger"; // UI at /swagger
    });

    // Auto-apply migrations in Development for a frictionless first run.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}
else
{
    // HSTS only outside Development — dev cert is self-signed.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Add hardening headers on every response.
app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseCors("WebClient");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// OutputCache must sit AFTER auth so policies can vary by Authorization,
// and BEFORE MapControllers so it can short-circuit cached responses.
app.UseOutputCache();

// Health-check endpoints — anonymous, no rate-limit, no output cache so
// orchestrators always get the live result.
app.MapHealthChecks("/health/live", new()
{
    Predicate = r => r.Tags.Contains("live"),
    ResponseWriter = HealthCheckResponseWriter.Write,
}).AllowAnonymous().DisableRateLimiting();

app.MapHealthChecks("/health/ready", new()
{
    Predicate = r => r.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponseWriter.Write,
}).AllowAnonymous().DisableRateLimiting();

app.MapControllers();

app.Run();

public partial class Program { }
