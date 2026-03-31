namespace Profile.API.DTOs;

public class ProfileResponse
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }          // lấy từ token JWT
    public string? PhoneNumber { get; set; }
    public string? Gender { get; set; }
    public string? DateOfBirth { get; set; }    // "YYYY-MM-DD"
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }

    public string? StudentCode { get; set; }
    public string? StudentYear { get; set; }
    public string? School { get; set; }
    public string? Faculty { get; set; }

    public string? CitizenId { get; set; }
    public string? CitizenIdIssuedPlace { get; set; }
    public string? Ethnicity { get; set; }
    public string? Religion { get; set; }

    public string? Province { get; set; }
    public string? District { get; set; }
    public string? Ward { get; set; }
    public string? AddressLine { get; set; }

    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhoneNumber { get; set; }
    public string? EmergencyContactAddress { get; set; }
}
