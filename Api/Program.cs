using Api.StartupConfigurations;
using Serilog;

namespace Api
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                Log.Debug("init main");

                var builder = WebApplication.CreateBuilder(args);

                builder.Host.UseSerilog();

                builder.AddOpenTelemetry("eComNetAPI");

                Startup.ConfigureServices(builder.Services, builder.Configuration, builder.Environment);

                var app = builder.Build();

                Startup.ConfigureApp(app, builder.Environment);

                await app.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Stopped program because of exception");
                //throw;
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
        }
    }
}
