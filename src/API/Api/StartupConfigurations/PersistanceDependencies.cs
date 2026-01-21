using Api.Helpers;
using Core.Interfaces;
using Data;
using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Api.StartupConfigurations
{
    public static class PersistanceDependencies
    {
        public static void AddDataPersistenceServices(this IServiceCollection services,
                                                      IConfiguration configuration, IWebHostEnvironment environment)
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

                x.LogTo(Serilog.Log.Information,
                    [DbLoggerCategory.Database.Command.Name],
                    LogLevel.Information);

                if (environment.IsDevelopment())
                {
                    x.AddInterceptors(new InlineSqlWithTimingInterceptor(
                        services.BuildServiceProvider()
                                .GetRequiredService<ILogger<InlineSqlWithTimingInterceptor>>()));

                    x.ConfigureWarnings(w => w.Ignore(RelationalEventId.CommandExecuting)
                                              .Ignore(RelationalEventId.CommandExecuted)
                                              .Ignore(RelationalEventId.CommandError));
                }
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

                x.LogTo(Serilog.Log.Information,
                        [DbLoggerCategory.Database.Command.Name],
                        LogLevel.Information);

                if (environment.IsDevelopment())
                {
                    x.AddInterceptors(new InlineSqlWithTimingInterceptor(
                        services.BuildServiceProvider()
                                .GetRequiredService<ILogger<InlineSqlWithTimingInterceptor>>()));

                    x.ConfigureWarnings(w => w.Ignore(RelationalEventId.CommandExecuting)
                                              .Ignore(RelationalEventId.CommandExecuted)
                                              .Ignore(RelationalEventId.CommandError));
                }
            });

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IGenericRepositoryResolver, GenericRepositoryResolver>();

            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IBasketRepository, BasketRepository>();
        }
    }
}