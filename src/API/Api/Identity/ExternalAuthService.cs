using System.Security.Claims;
using Core.Entities.Identity;
using Microsoft.AspNetCore.Identity;

namespace Api.Identity;

/// <summary>
/// Implementation of external authentication service for social login.
/// </summary>
public class ExternalAuthService(
    UserManager<AppUser> userManager,
    IAuthEventsLog authEventsLog,
    ILogger<ExternalAuthService> logger) : IExternalAuthService
{
    public async Task<ExternalAuthResult> FindOrCreateUserAsync(ExternalLoginInfo info)
    {
        // First, try to find user by external login
        var user = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);

        if (user != null)
        {
            logger.LogInformation("User {UserId} found by external login {Provider}", user.Id, info.LoginProvider);
            return ExternalAuthResult.Success(user);
        }

        // Get email from claims
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrEmpty(email))
        {
            logger.LogWarning("No email claim provided by {Provider}", info.LoginProvider);
            return ExternalAuthResult.Failure("No email address was provided by the external provider.");
        }

        // Check if user exists with this email
        user = await userManager.FindByEmailAsync(email);

        if (user != null)
        {
            // Auto-link: User exists with same email, link the external login
            // We trust Google's email verification
            var linkResult = await userManager.AddLoginAsync(user, info);

            if (!linkResult.Succeeded)
            {
                var errors = string.Join(", ", linkResult.Errors.Select(e => e.Description));
                logger.LogWarning("Failed to auto-link {Provider} to user {UserId}: {Errors}",
                    info.LoginProvider, user.Id, errors);
                return ExternalAuthResult.Failure($"Failed to link account: {errors}");
            }

            authEventsLog.Monitor_ExternalAccountLinked(user.Id, user.Email, info.LoginProvider, null);
            logger.LogInformation("Auto-linked {Provider} to existing user {UserId} with email {Email}",
                info.LoginProvider, user.Id, email);

            return ExternalAuthResult.Success(user, isNewUser: false, wasLinked: true);
        }

        // Create new user
        var displayName = info.Principal.FindFirstValue(ClaimTypes.Name)
            ?? info.Principal.FindFirstValue(ClaimTypes.GivenName)
            ?? email.Split('@')[0];

        user = new AppUser
        {
            Email = email,
            UserName = email,
            DisplayName = displayName,
            EmailConfirmed = true // Google has verified the email
        };

        var createResult = await userManager.CreateAsync(user);

        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            logger.LogWarning("Failed to create user for {Provider}: {Errors}", info.LoginProvider, errors);
            return ExternalAuthResult.Failure($"Failed to create account: {errors}");
        }

        // Link the external login to the new user
        var addLoginResult = await userManager.AddLoginAsync(user, info);

        if (!addLoginResult.Succeeded)
        {
            // Rollback user creation
            await userManager.DeleteAsync(user);

            var errors = string.Join(", ", addLoginResult.Errors.Select(e => e.Description));
            logger.LogWarning("Failed to add {Provider} login to new user: {Errors}", info.LoginProvider, errors);
            return ExternalAuthResult.Failure($"Failed to link external login: {errors}");
        }

        authEventsLog.Monitor_ExternalLogin(user.Id, user.Email, info.LoginProvider, null);
        logger.LogInformation("Created new user {UserId} via {Provider} with email {Email}",
            user.Id, info.LoginProvider, email);

        return ExternalAuthResult.Success(user, isNewUser: true);
    }

    public async Task<ExternalAuthResult> LinkExternalLoginAsync(AppUser user, ExternalLoginInfo info)
    {
        // Check if already linked
        var existingLogins = await userManager.GetLoginsAsync(user);
        if (existingLogins.Any(l => l.LoginProvider == info.LoginProvider))
        {
            return ExternalAuthResult.Failure($"Your account is already linked to {info.ProviderDisplayName ?? info.LoginProvider}.");
        }

        // Check if this external login is already linked to another account
        var existingUser = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
        if (existingUser != null && existingUser.Id != user.Id)
        {
            logger.LogWarning("External login {Provider}:{ProviderKey} is already linked to user {OtherUserId}",
                info.LoginProvider, info.ProviderKey, existingUser.Id);
            return ExternalAuthResult.Failure($"This {info.ProviderDisplayName ?? info.LoginProvider} account is already linked to another user.");
        }

        var result = await userManager.AddLoginAsync(user, info);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogWarning("Failed to link {Provider} to user {UserId}: {Errors}",
                info.LoginProvider, user.Id, errors);
            return ExternalAuthResult.Failure($"Failed to link account: {errors}");
        }

        authEventsLog.Monitor_ExternalAccountLinked(user.Id, user.Email, info.LoginProvider, null);
        logger.LogInformation("Linked {Provider} to user {UserId}", info.LoginProvider, user.Id);

        return ExternalAuthResult.Success(user, wasLinked: true);
    }

    public async Task<ExternalAuthResult> UnlinkExternalLoginAsync(AppUser user, string provider, string providerKey)
    {
        // Check if user has other authentication methods
        var hasPassword = await userManager.HasPasswordAsync(user);
        var logins = await userManager.GetLoginsAsync(user);

        if (!hasPassword && logins.Count <= 1)
        {
            return ExternalAuthResult.Failure("Cannot unlink your only login method. Please set a password first.");
        }

        var result = await userManager.RemoveLoginAsync(user, provider, providerKey);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogWarning("Failed to unlink {Provider} from user {UserId}: {Errors}",
                provider, user.Id, errors);
            return ExternalAuthResult.Failure($"Failed to unlink account: {errors}");
        }

        authEventsLog.Monitor_ExternalAccountUnlinked(user.Id, user.Email, provider, null);
        logger.LogInformation("Unlinked {Provider} from user {UserId}", provider, user.Id);

        return ExternalAuthResult.Success(user);
    }

    public async Task<IList<UserLoginInfo>> GetExternalLoginsAsync(AppUser user)
    {
        return await userManager.GetLoginsAsync(user);
    }

    public async Task<bool> HasPasswordAsync(AppUser user)
    {
        return await userManager.HasPasswordAsync(user);
    }
}
