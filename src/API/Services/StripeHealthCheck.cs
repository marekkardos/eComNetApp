using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Stripe;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Services;

public class StripeHealthCheck(IConfiguration configuration, ILogger<StripeHealthCheck> logger) : IHealthCheck
{
    private readonly string _stripeApiKey = configuration["StripeSettings:SecretKey"] 
        ?? throw new InvalidOperationException("Missing \"StripeSettings:SecretKey\" configuration.");

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_stripeApiKey))
            {
                logger.LogWarning("Stripe API key is not configured");
                return HealthCheckResult.Unhealthy("Stripe API key is not configured");
            }

            StripeConfiguration.ApiKey = _stripeApiKey;

            var balanceService = new BalanceService();
            var balance = await balanceService.GetAsync(cancellationToken: cancellationToken);

            logger.LogDebug("Stripe health check succeeded. Available balance: {Currency}",
                balance.Available?.FirstOrDefault()?.Currency ?? "unknown");

            return HealthCheckResult.Healthy("Stripe API is reachable and API key is valid");
        }
        catch (StripeException stripeEx)
        {
            logger.LogWarning(stripeEx, "Stripe API returned an error: {Message}", stripeEx.Message);

            // Authentication errors mean the key is invalid - this is Unhealthy
            if (stripeEx.StripeError?.Type == "invalid_request_error")
            {
                return HealthCheckResult.Unhealthy(
                    $"Stripe API key is invalid: {stripeEx.StripeError.Message}",
                    stripeEx);
            }

            // Other Stripe errors might be temporary - mark as Degraded
            return HealthCheckResult.Degraded(
                $"Stripe API error: {stripeEx.StripeError?.Message ?? stripeEx.Message}",
                stripeEx);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Stripe health check failed with unexpected error");

            // Network issues or other temporary problems - mark as Degraded, not Unhealthy
            return HealthCheckResult.Degraded(
                $"Unable to reach Stripe API: {ex.Message}",
                ex);
        }
    }
}
