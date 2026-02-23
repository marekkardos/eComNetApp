namespace Api.Identity;

internal class AuthCodeData
{
    public string UserId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
