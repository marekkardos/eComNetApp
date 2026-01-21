// See https://aka.ms/new-console-template for more information

using Core.Entities.Identity;
using Data;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SeedData;

namespace MyProject;

static class Program
{
    static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        Console.WriteLine("Data seeding started.");

        await SeedData(host);

        Console.WriteLine("Data seeding completed!");
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                ConfigureServices(services);
            });

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddDbContext<StoreContext>(x =>
        {
            x.UseSqlServer(
                "name=ConnectionStrings:DefaultConnectionMssql",
                sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
                });
        });

        services.AddDbContext<AppIdentityDbContext>(x =>
        {
            x.UseSqlServer(
                "name=ConnectionStrings:IdentityConnectionMssql",
                sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
                });
        });

        services.AddIdentityCore<AppUser>()
                .AddEntityFrameworkStores<AppIdentityDbContext>();
    }

    private static async Task SeedData(IHost host)
    {
        using (var scope = host.Services.CreateScope())
        {
            var services = scope.ServiceProvider;

            var loggerFactory = services.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger(nameof(Program));

            try
            {
                var userManager = services.GetRequiredService<UserManager<AppUser>>();
                var identityContext = services.GetRequiredService<AppIdentityDbContext>();
                await identityContext.Database.MigrateAsync();
                await AppIdentityDbContextSeed.SeedUsersAsync(userManager);
                logger.LogInformation("AppIdentityDbContextSeed was invoked.");

                var storeCtx = services.GetRequiredService<StoreContext>();
                await storeCtx.Database.MigrateAsync();
                await StoreContextSeed.SeedAsync(storeCtx, loggerFactory);
                logger.LogInformation("StoreContextSeed was invoked.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during migration.");
            }
        }
    }
}
