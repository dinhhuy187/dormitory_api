namespace Profile.API.Domain.Entities;

public class UserProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;

    // Thông tin cơ bản
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Gender { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }

    // Thông tin sinh viên
    public string? StudentCode { get; set; }
    public string? StudentYear { get; set; }
    public string? School { get; set; }
    public string? Faculty { get; set; }

    // Giấy tờ cá nhân
    public string? CitizenId { get; set; }
    public string? CitizenIdIssuedPlace { get; set; }
    public string? Ethnicity { get; set; }
    public string? Religion { get; set; }

    // Địa chỉ
    public string? Province { get; set; }
    public string? District { get; set; }
    public string? Ward { get; set; }
    public string? AddressLine { get; set; }

    // Liên hệ khẩn cấp
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhoneNumber { get; set; }
    public string? EmergencyContactAddress { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}