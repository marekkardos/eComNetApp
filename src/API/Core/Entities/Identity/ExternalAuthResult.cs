namespace Core.Entities.Identity;

/// <summary>
/// Result of an external authentication operation.
/// </summary>
public class ExternalAuthResult
{
    public bool Succeeded { get; set; }
    public AppUser User { get; set; }
    public string Error { get; set; }
    public bool IsNewUser { get; set; }
    public bool WasLinked { get; set; }

    public static ExternalAuthResult Success(AppUser user, bool isNewUser = false, bool wasLinked = false)
        => new() { Succeeded = true, User = user, IsNewUser = isNewUser, WasLinked = wasLinked };

    public static ExternalAuthResult Failure(string error)
        => new() { Succeeded = false, Error = error };
}
