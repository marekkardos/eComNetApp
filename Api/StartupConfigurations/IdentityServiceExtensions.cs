using Api.Identity;
using System.Text;
using Core.Entities.Identity;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Api.StartupConfigurations;

public static class IdentityServiceExtensions
{
    public static void AddCustomIdentityServices(this IServiceCollection services, IConfiguration config)
    {
        var key = config["Token:Key"] ?? throw new InvalidOperationException("Token Key is missing");
        var keyBytes = Encoding.UTF8.GetBytes(key);

        var builder = services.AddIdentityCore<AppUser>();

        builder = new IdentityBuilder(builder.UserType, builder.Services);
        builder.AddEntityFrameworkStores<AppIdentityDbContext>();
        builder.AddSignInManager<SignInManager<AppUser>>();

        // Read lockout settings from configuration (uses defaults if section is missing)
        var lockoutSettings = config.GetSection("LockoutSettings").Get<LockoutSettings>() ?? new LockoutSettings();

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
            });
    }
}
