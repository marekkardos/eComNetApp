using System;
using System.ComponentModel.DataAnnotations;

namespace Core.Entities.Identity;

public class RefreshToken
{
    public int Id { get; set; }

    [Required]
    public string Token { get; set; }

    [Required]
    public string JwtId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public bool IsRevoked { get; set; }

    public DateTime? RevokedAt { get; set; }

    public string ReplacedByToken { get; set; }

    [Required]
    public string AppUserId { get; set; }

    public AppUser AppUser { get; set; }
}
