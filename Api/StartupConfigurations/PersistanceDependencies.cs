using Core.Interfaces;
using Data;
using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Api.StartupConfigurations
{
    public static class PersistanceDependencies
    {
        public static void AddDataPersistenceServices(this IServiceCollection services, IConfiguration configuration)
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
            });

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IGenericRepositoryResolver, GenericRepositoryResolver>();

            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IBasketRepository, BasketRepository>();

            AddHealthChecks(services);
        }

        private static void AddHealthChecks(IServiceCollection services)
        {
            //services.AddHealthChecks()
            //            .AddCheck<DbContextHealthCheck<StoreContext>>("StoreContextCheck");
        }
    }
}