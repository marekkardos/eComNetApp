using System;
using Core.Interfaces;
using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Data
{
    public static class ConfigureDependencies
    {
        public static void AddDataPersistenceServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<StoreContext>(x =>
                {
                    // x.UseSqlite(configuration.GetConnectionString("DefaultConnection")));
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
                // x.UseSqlite(configuration.GetConnectionString("IdentityConnection"));
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

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IGenericRepositoryResolver, GenericRepositoryResolver>();

            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IBasketRepository, BasketRepository>();
        }
    }
}