using Api;
using Api.StartupConfigurations;
using Serilog;
using Serilog.Core;

try
{
    Log.Debug("init main");

    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.AddServiceDefaults();

    // Create dedicated audit logger for critical security events
    // Uses AuditTo.Seq() which:
    // - Throws exceptions on write failure (guaranteed delivery)
    // - Sends events synchronously (blocking network calls)
    // - Should only be used for critical security events due to performance impact
    Logger auditLogger = new LoggerConfiguration()
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "eComNetApp_API")
        .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
        .Enrich.WithProperty("LogType", "SecurityAudit")
        .AuditTo.Seq(
            serverUrl: builder.Configuration["Seq:ServerUrl"] 
                        ?? throw new InvalidOperationException("Seq:ServerUrl configuration is missing."),
            apiKey: builder.Configuration["Seq:ApiKey"]
        )
        .CreateLogger();

    // Register audit logger in DI container for injection into services
    builder.Services.AddSingleton(auditLogger);

    // Standard logging is configured in LoggingExtensions.AddLogging()
    Startup.ConfigureServices(builder.Services, builder.Configuration, builder.Environment);

    builder.AddOpenTelemetry("eComNetAPI");

    WebApplication app = builder.Build();

    app.MapDefaultEndpoints();

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
