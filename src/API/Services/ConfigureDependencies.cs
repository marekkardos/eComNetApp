using Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Services.Interfaces;
using System;

namespace Services;

public static class ConfigureDependencies
{
    public static void AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        var stripeSettingsSecretKey = configuration["StripeSettings:SecretKey"]
            ?? throw new InvalidOperationException("StripeSettings SecretKey is missing");

        services.AddSingleton<IResponseCacheService, ResponseCacheService>();

        services.AddScoped<IPaymentService, StripePaymentService>(p =>
        {
            var unitOfWork = p.GetService<IUnitOfWork>()
                ?? throw new InvalidOperationException("IUnitOfWork is not registered");

            var basket = p.GetService<IBasketRepository>()
                ?? throw new InvalidOperationException("IBasketRepository is not registered");

            var logger = p.GetService<ILogger<StripePaymentService>>()
                ?? throw new InvalidOperationException("Logger for StripePaymentService is not registered");

            return new StripePaymentService(basket, unitOfWork, stripeSettingsSecretKey, logger);
        });
    }
}