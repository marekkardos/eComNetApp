using Api;
using Api.StartupConfigurations;
using Serilog;

try
{
    Log.Debug("init main");

    var builder = WebApplication.CreateBuilder(args);

    // Create dedicated audit logger for critical security events
    // Uses AuditTo.Seq() which:
    // - Throws exceptions on write failure (guaranteed delivery)
    // - Sends events synchronously (blocking network calls)
    // - Should only be used for critical security events due to performance impact
    var seqServerUrl = builder.Configuration["Seq:ServerUrl"];

    Serilog.Core.Logger auditLogger;
    if (!string.IsNullOrEmpty(seqServerUrl))
    {
        auditLogger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "eComNetApp_API")
            .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
            .Enrich.WithProperty("LogType", "SecurityAudit")
            .AuditTo.Seq(
                serverUrl: seqServerUrl,
                apiKey: builder.Configuration["Seq:ApiKey"]
            )
            .CreateLogger();
    }
    else
    {
        // In Testing environment or when Seq is not configured, use a console-based audit logger
        auditLogger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "eComNetApp_API")
            .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
            .Enrich.WithProperty("LogType", "SecurityAudit")
            .WriteTo.Console()
            .CreateLogger();

        if (!builder.Environment.EnvironmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase))
        {
            Log.Warning("Seq:ServerUrl configuration is missing. Audit logging will use console output.");
        }
    }

    // Register audit logger in DI container for injection into services
    builder.Services.AddSingleton(auditLogger);

    // Standard logging is configured in LoggingExtensions.AddLogging()
    Startup.ConfigureServices(builder.Services, builder.Configuration, builder.Environment);

    builder.AddOpenTelemetry("eComNetAPI");

    var app = builder.Build();

    Startup.ConfigureApp(app, builder.Environment);

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Error(ex, "Stopped program because of exception");
}
finally
{
    await Log.CloseAndFlushAsync();
}
