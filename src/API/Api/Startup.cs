using Api.StartupConfigurations;
using Api.Helpers;
using Api.Identity;
using Core.Entities;
using Core.Interfaces;
using MediatR;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Services;
using StackExchange.Redis;
using Api.ApiResponses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.DataProtection;

namespace Api;

public static class Startup
{
    public static void ConfigureServices(IServiceCollection services, IConfiguration conf, IWebHostEnvironment environment)
    {
        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo("dataprotection-keys"))
            .SetApplicationName("eComNetApi");

        services.AddLogging(conf, environment);

        string redisConnection = conf.GetConnectionString("Redis")
                                ?? throw new InvalidOperationException("Redis connection string is missing");
        services.AddSingleton<IConnectionMultiplexer>(c =>
        {
            var configuration = ConfigurationOptions.Parse(redisConnection, true);
            return ConnectionMultiplexer.Connect(configuration);
        });

        services.AddCors(opt =>
        {
            opt.AddDefaultPolicy(policy =>
            {
                var allowedOrigins = conf.GetValue<string>("AllowedOrigins")?
                                         .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                         ?? ["http://localhost:4200"];

                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        services.AddControllers(m =>
            {
                // e.g application/xml
                m.ReturnHttpNotAcceptable = true;
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            })
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = actionContext =>
                {
                    string[] errors = actionContext.ModelState
                        .Where(e => e.Value.Errors.Count > 0)
                        .SelectMany(x => x.Value.Errors)
                        .Select(x => x.ErrorMessage).ToArray();

                    var errorResponse = new ApiValidationErrorResponse
                    {
                        Errors = errors
                    };

                    ILogger<ApiValidationErrorResponse> logger = services.BuildServiceProvider()
                                                                        .GetRequiredService<ILogger<ApiValidationErrorResponse>>();

                    logger.LogInformation("Validation errors occurred: {Errors}", string.Join(Environment.NewLine, errors));

                    return new BadRequestObjectResult(errorResponse);
                };
            });

        services.AddMediatR(typeof(BaseEntity));
        services.AddAutoMapperServiceExt();

        services.AddDataPersistenceServices(conf, environment);
        services.AddCustomIdentityServices(conf, environment);

        services.AddEndpointsApiExplorer();
        services.AddSwaggerServicesExt();

        services.AddCustomApiVersioning();
        services.AddScoped<IPictureUrlResolver, PictureUrlResolver>();

        services.Configure<TokenSettings>(conf.GetSection("TokenSettings"));
        services.Configure<GoogleAuthSettings>(conf.GetSection("GoogleAuth"));
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IAuthEventsLog, AuthEventsLog>();
        services.AddScoped<IAuthenticationServices, AuthenticationServices>();
        services.AddScoped<IExternalAuthCodeService, ExternalAuthCodeService>();
        services.AddScoped<IExternalAuthService, ExternalAuthService>();
        services.AddApplicationServices(conf);

        services.AddHealthChecksExt(conf);
    }

    public static void ConfigureApp(WebApplication app, IWebHostEnvironment env)
    {
        // Warn if AllowedReturnUrlHosts is still the default ["localhost"] in a non-Development environment.
        // Google login silently falls back to '/' for any non-localhost returnUrl in that case.
        if (!app.Environment.IsDevelopment())
        {
            var googleSettings = app.Services.GetRequiredService<IOptions<GoogleAuthSettings>>().Value;
            if (googleSettings.AllowedReturnUrlHosts.Length == 1 &&
                string.Equals(googleSettings.AllowedReturnUrlHosts[0], "localhost", StringComparison.OrdinalIgnoreCase))
            {
                app.Services.GetRequiredService<ILogger<GoogleAuthSettings>>()
                    .LogWarning(
                        "GoogleAuth:AllowedReturnUrlHosts is set to [\"localhost\"] only. " +
                        "Google login will silently fail for non-localhost return URLs. " +
                        "Configure AllowedReturnUrlHosts in appsettings.{EnvironmentName}.json.",
                        app.Environment.EnvironmentName);
            }
        }

        if (app.Environment.IsDevelopment())
        {
            app.AssertAutoMapperConfigurationIsValid();
        }
        else
        {
            app.UseHsts();
        }

        app.UseHealthChecksExt();

        app.UseCustomLogging();

        app.UseExceptionHandler("/error");
        app.UseStatusCodePagesWithReExecute("/errors/{0}");

        app.UseHttpsRedirection();

        // allow wwwroot
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(
                Path.Combine(Directory.GetCurrentDirectory(), "Content")
            ),
            RequestPath = "/content"
        });

        app.UseRouting();

        // CORS headers are only sent on cross domain requests and the
        // ASP.NET CORS module is smart enough to detect whether a same domain request
        // is firing and if it is, doesn't send the headers. 
        // test with: testCORS.html in Api folder
        app.UseCors();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseSwaggerExt();

        app.MapControllers();
    }
}