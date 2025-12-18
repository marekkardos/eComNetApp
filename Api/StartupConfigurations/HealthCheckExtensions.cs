using Data;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;

namespace Api.StartupConfigurations;

public static class HealthCheckExtensions
{
    public static void AddHealthChecksExt(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
                .AddDbContextCheck<StoreContext>(name: "StoreDbContext")
                .AddDbContextCheck<AppIdentityDbContext>(name: "AppIdentityDbContext")
                .AddRedis(configuration["ConnectionStrings:Redis"], name: "redis cache");
    }

    public static void UseHealthChecksExt(this WebApplication app)
    {
        var allowedLocalIPs = new[] { "127.0.0.1", "::1", "localhost" };

        app.MapHealthChecks("/healthz", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";

                var result = JsonSerializer.Serialize(new
                {
                    status = report.Status.ToString(),
                    totalDuration = report.TotalDuration.ToString(),
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        duration = e.Value.Duration.ToString(),
                        description = e.Value.Description,
                        exception = e.Value.Exception?.Message,
                        data = e.Value.Data
                    })
                },
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await context.Response.WriteAsync(result);
            }
        })
        .RequireAuthorization(policy =>
        {
            policy.RequireAssertion(context =>
            {
                var httpCtx = context.Resource as HttpContext;

                var logger = httpCtx.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("HealthCheck");

                var remoteIp =  httpCtx.Connection.RemoteIpAddress?.ToString();

                if (allowedLocalIPs.Contains(remoteIp))
                {
                    logger.LogInformation("Health check accessed from localhost ({RemoteIP})", remoteIp);
                    return true;
                }

                var providedKey = httpCtx?.Request.Headers["X-Health-Check-Key"].FirstOrDefault();
                var validKey = app.Configuration["HealthCheckApiKey"];

                if (string.IsNullOrWhiteSpace(validKey))
                {
                    logger.LogWarning("Health check API key is not configured. Access denied for {RemoteIP}", remoteIp);
                    return false;
                }

                if (!string.IsNullOrEmpty(providedKey))
                {
                    if (string.CompareOrdinal(providedKey, validKey) == 0)
                    {
                        logger.LogInformation("Health check accessed with valid API key from {RemoteIP}", remoteIp);
                        return true;
                    }

                    logger.LogWarning("Health check unauthorized attempt with invalid API key from {RemoteIP}", remoteIp);
                }
                else
                {
                    logger.LogWarning("Health check unauthorized attempt without API key from {RemoteIP}", remoteIp);
                }

                return false;
            });
        });
    }
}
