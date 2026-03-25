using System.ComponentModel.DataAnnotations;

namespace Identity.API.DTOs;

public class UpdateProfileRequestDto
{
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters")]
    public string? FullName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid phone number format")]
    public string? PhoneNumber { get; set; }

    [RegularExpression("^(Male|Female|Other)$", ErrorMessage = "Gender must be Male, Female or Other")]
    public string? Gender { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    [StringLength(100, ErrorMessage = "Bio cannot exceed 500 characters")]
    public string? Bio { get; set; }
}