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

                if (environment.IsDevelopment())
                {
                    string seqEndpoint = configuration["SEQ:ENDPOINT"] ?? string.Empty;

                    if (!string.IsNullOrWhiteSpace(seqEndpoint))
                    {
                        log.WriteTo.Seq(seqEndpoint, restrictedToMinimumLevel: LogEventLevel.Debug);
                    }
                }
            });
        }

        public static void UseCustomLogging(this IApplicationBuilder app)
        {
            //app.UseHttpLogging();

            app.UseSerilogRequestLogging(o =>
            {
                o.GetLevel = (httpContext, elapsed, ex) =>
                {
                    if (ex != null || httpContext.Response.StatusCode >= 500)
                    {
                        return LogEventLevel.Error;
                    }

                    if (httpContext.Response.StatusCode >= 400)
                    {
                        return LogEventLevel.Warning;
                    }

                    return LogEventLevel.Information;
                };
            });
        }
    }
}
