using Api.Identity;
using System.Text;
using Core.Entities.Identity;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Api.StartupConfigurations;

public static class IdentityServiceExtensions
{
    public static void AddCustomIdentityServices(this IServiceCollection services, IConfiguration config, IWebHostEnvironment environment)
    {
        var key = config["Token:Key"] ?? throw new InvalidOperationException("Token Key is missing");
        var keyBytes = Encoding.UTF8.GetBytes(key);

        var builder = services.AddIdentityCore<AppUser>();

        builder = new IdentityBuilder(builder.UserType, builder.Services);
        builder.AddEntityFrameworkStores<AppIdentityDbContext>();
        builder.AddSignInManager<SignInManager<AppUser>>();

        // Read lockout settings from configuration (uses defaults if section is missing)
        var lockoutSettings = config.GetSection("LockoutSettings").Get<LockoutSettings>() ?? new LockoutSettings();

        GoogleAuthSettings googleSettings = config.GetSection("GoogleAuth").Get<GoogleAuthSettings>()
                    ?? throw new InvalidOperationException("GoogleAuth configuration is missing");

        // Register LockoutSettings for DI injection if needed elsewhere
        services.Configure<LockoutSettings>(config.GetSection("LockoutSettings"));

        services.Configure<IdentityOptions>(options =>
        {
            // Password settings.
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 6;
            options.Password.RequiredUniqueChars = 1;

            // Lockout settings.
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(lockoutSettings.DefaultLockoutTimeSpanMinutes);
            options.Lockout.MaxFailedAccessAttempts = lockoutSettings.MaxFailedAccessAttempts;
            options.Lockout.AllowedForNewUsers = lockoutSettings.AllowedForNewUsers;

            // User settings.
            options.User.AllowedUserNameCharacters =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            options.User.RequireUniqueEmail = true;
        });

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                    ValidIssuer = config["Token:Issuer"],
                    ValidateIssuer = true,
                    ValidateAudience = false
                };
            })
            // Register the specific "Identity.External" scheme that SignInManager needs
            .AddCookie(IdentityConstants.ExternalScheme, options =>
            {
                options.Cookie.Name = IdentityConstants.ExternalScheme;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddGoogle(options =>
            {
                options.ClientId = googleSettings.ClientId;
                options.ClientSecret = googleSettings.ClientSecret;

                options.SignInScheme = IdentityConstants.ExternalScheme;

                // Request email and profile scopes
                options.Scope.Add("email");
                options.Scope.Add("profile");

                // Configure the callback path for OAuth flow
                options.CallbackPath = "/signin-google";

                // Configure cookie policy for OAuth correlation cookies
                options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
                options.CorrelationCookie.SameSite = SameSiteMode.Lax;
            });
    }
}
