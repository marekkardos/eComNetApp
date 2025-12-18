using Api;
using Api.StartupConfigurations;
using Serilog;

try
{
    Log.Debug("init main");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

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