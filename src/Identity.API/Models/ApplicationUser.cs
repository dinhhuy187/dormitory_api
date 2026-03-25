using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

public class ApplicationUser : IdentityUser
{
    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;
    public string? Gender { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    [MaxLength(500)]
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string? StudentCode { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTimeUtc { get; set; }
    public string? GoogleProviderKey { get; set; }
    public bool IsActive { get; set; } = true;
}
