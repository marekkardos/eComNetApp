namespace Api.Identity;

public class LockoutSettings
{
    /// <summary>
    /// How long a user is locked out after reaching the max failed access attempts.
    /// Default: 15 minutes
    /// </summary>
    public int DefaultLockoutTimeSpanMinutes { get; set; } = 15;

    /// <summary>
    /// Number of failed access attempts before a user is locked out.
    /// Default: 5 attempts
    /// </summary>
    public int MaxFailedAccessAttempts { get; set; } = 5;

    /// <summary>
    /// Whether lockout is enabled for new users.
    /// Default: true
    /// </summary>
    public bool AllowedForNewUsers { get; set; } = true;
}
