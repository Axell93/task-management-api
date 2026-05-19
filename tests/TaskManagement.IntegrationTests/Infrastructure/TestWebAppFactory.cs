using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.IntegrationTests.Infrastructure;

/// <summary>
/// Boots Program.cs in-process but swaps SQL Server for an InMemory database
/// so the tests have no external dependencies. A fresh DB name per factory
/// keeps test classes isolated.
/// </summary>
public class TestWebAppFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"tasks-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Production); // avoid auto-Migrate() of SQL Server

        // UseSetting writes into the *host's* configuration before Program.cs
        // reads it — this is the only reliable way to inject values that the
        // initial pipeline (JwtBearer setup) must see.
        builder.UseSetting("Jwt:Issuer", "test-issuer");
        builder.UseSetting("Jwt:Audience", "test-audience");
        builder.UseSetting("Jwt:Key", "0123456789abcdef0123456789abcdef-test-key-extra");
        builder.UseSetting("Jwt:ExpiryMinutes", "30");
        builder.UseSetting("FeatureFlags:InfoLoggingEnabled", "false");
        builder.UseSetting("ConnectionStrings:DefaultConnection", "ignored-but-required");
        // Effectively disable the rate limiter so the test suite isn't
        // throttled — the limiter is still exercised by a dedicated test.
        builder.UseSetting("RateLimit:Auth:PermitLimit", "100000");
        builder.UseSetting("RateLimit:Tasks:PermitLimit", "100000");

        builder.ConfigureServices(services =>
        {
            // Strip out the SQL Server registration (DbContextOptions + the
            // configurator that wires the SqlServer provider) and re-register
            // AppDbContext on the InMemory provider.
            RemoveAll<DbContextOptions<AppDbContext>>(services);
            RemoveAll<DbContextOptions>(services);
            RemoveAll<AppDbContext>(services);
            RemoveAll<DbConnection>(services);
            foreach (var d in services
                .Where(s => s.ServiceType.FullName?.StartsWith("Microsoft.EntityFrameworkCore.IDbContextOptionsConfiguration") == true)
                .ToList())
            {
                services.Remove(d);
            }

            // Isolate EF's internal service provider so SQL Server (still in the
            // assembly load context) doesn't conflict with InMemory.
            var efSp = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            services.AddDbContext<AppDbContext>(opt => opt
                .UseInMemoryDatabase(_dbName)
                .UseInternalServiceProvider(efSp));

            // The repository's GetSummaryAsync uses ADO.NET directly — the
            // InMemory provider does not support that, so swap to a stub.
            RemoveAll<ITaskRepository>(services);
            RemoveAll<ITaskSummaryQuery>(services);
            services.AddScoped<ITaskRepository, InMemoryTaskRepository>();

            using var scope = services.BuildServiceProvider().CreateScope();
            scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
        });
    }

    private static void RemoveAll<T>(IServiceCollection services)
    {
        foreach (var d in services.Where(s => s.ServiceType == typeof(T)).ToList())
            services.Remove(d);
    }
}
