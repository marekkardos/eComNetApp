using Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Services.Interfaces;

namespace Services
{
    public static class ConfigureDependencies
    {
        public static void AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IResponseCacheService, ResponseCacheService>();

            services.AddScoped<IPaymentService, StripePaymentService>();
        }
    }
}