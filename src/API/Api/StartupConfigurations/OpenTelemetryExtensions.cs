using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Reflection;

namespace Api.StartupConfigurations
{
    public static class OpenTelemetryExtensions
    {
        public static WebApplicationBuilder AddOpenTelemetry(this WebApplicationBuilder builder, string serviceName)
        {
            builder.Logging.AddOpenTelemetry(x =>
            {
                x.IncludeScopes = true;
                x.IncludeFormattedMessage = true;
            });

            var otelEndpoint = builder.Configuration["OPEN_TELEMETRY:ENDPOINT"]
                ?? builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
                ?? throw new InvalidOperationException(
                    "Missing required configuration: OPEN_TELEMETRY:ENDPOINT or OTEL_EXPORTER_OTLP_ENDPOINT");

            builder.Services.AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService(
                    serviceName: builder.Environment.ApplicationName,
                    serviceVersion: Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "UNKNOWN")
                    .AddAttributes(
                        [
                            new KeyValuePair<string, object>("host.name", Environment.MachineName),
                            new KeyValuePair<string, object>("environment.name", builder.Environment.EnvironmentName)
                        ]))
                .WithTracing(tracerBuilder =>
                {
                    tracerBuilder.AddSource(serviceName)
#if RELEASE
                                 .SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(0.1)))
#endif
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation();

                    tracerBuilder.AddOtlpExporter(opt =>
                    {
                        opt.Endpoint = new Uri(otelEndpoint);
                        opt.Protocol = OtlpExportProtocol.Grpc;
                    });
                })
                .WithMetrics(metricsBuilder =>
                    {
                        metricsBuilder
                        //.AddMeter(serviceName) // Your custom meter
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddProcessInstrumentation()
                        //.AddPrometheusExporter()
                        .AddOtlpExporter((exporterOptions, metricReaderOptions) =>
                         {
                             exporterOptions.Endpoint = new Uri(otelEndpoint);
                             exporterOptions.Protocol = OtlpExportProtocol.Grpc;
#if DEBUG
                             // Default collection interval is 60 seconds
                             metricReaderOptions.PeriodicExportingMetricReaderOptions = new()
                             {
                                 ExportIntervalMilliseconds = 10000, // 10 seconds
                                 ExportTimeoutMilliseconds = 30000
                             };
#endif
                         });
                    }
                );

            return builder;
        }
    }
}
