using System.ComponentModel.DataAnnotations;

namespace Profile.API.Models;

public class UserProfile
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    // Liên kết với Identity — chỉ lưu UserId (string), KHÔNG foreign key sang DB khác
    [Required]
    public string UserId { get; set; } = string.Empty;

    // Thông tin cơ bản
    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Gender { get; set; }         // "Male" | "Female" | "Other"
    public DateOnly? DateOfBirth { get; set; }
    [MaxLength(500)]
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