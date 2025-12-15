using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Events;
using Serilog.Sinks.OpenTelemetry;
using System.Reflection;

namespace Api.StartupConfigurations
{
    public static class LoggingExtensions
    {
        public static void AddLogging(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
        {
            services.AddSerilog(log =>
            {
                log.Filter.ByExcluding("RequestPath like '%/health%'")
                   .Filter.ByExcluding("RequestPath like '%/swagger%'");

                log.Enrich.WithSpan()
                   .Enrich.FromLogContext()
                   .WriteTo.Console();

                string openTelemetryEndpoint = configuration["OPEN_TELEMETRY:ENDPOINT"] ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(openTelemetryEndpoint))
                {
                    log.WriteTo.OpenTelemetry(opts =>
                    {
                        opts.Endpoint = openTelemetryEndpoint;
                        opts.Protocol = OtlpProtocol.Grpc;
                        opts.RestrictedToMinimumLevel = LogEventLevel.Information;

                        opts.ResourceAttributes = new Dictionary<string, object>
                        {
                            ["service.name"] = Assembly.GetEntryAssembly()?.GetName().Name ?? "UnknownServiceName",
                            ["service.version"] = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "UnknownServiceVersion",
                            ["deployment.environment"] = environment.EnvironmentName
                        };
                    });
                }
            });
        }
    }
}
