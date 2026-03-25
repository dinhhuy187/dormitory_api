namespace Identity.API.DTOs;

public class ProfileResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Gender { get; set; }
    public string? DateOfBirth { get; set; } // Format YYYY-MM-DD
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string? StudentCode { get; set; }

}
