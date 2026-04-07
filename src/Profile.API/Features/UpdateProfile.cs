using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Profile.API.Domain.Entities;
using Profile.API.Infrastructure.Database;
using Shared;
using Shared.Endpoints;
using System.Security.Claims;

namespace Profile.API.Features.Profile;

public static class UpdateProfile
{
    public record Command(
        string? FullName,
        string? PhoneNumber,
        string? Gender,
        string? DateOfBirth,
        string? Bio,
        string? StudentYear,
        string? School,
        string? Faculty,
        string? CitizenId,
        string? CitizenIdIssuedPlace,
        string? Ethnicity,
        string? Religion,
        string? Province,
        string? District,
        string? Ward,
        string? AddressLine,
        string? EmergencyContactName,
        string? EmergencyContactPhoneNumber,
        string? EmergencyContactAddress
    );

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            // ===== FullName =====
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Họ tên không được để trống.")
                .MaximumLength(100).WithMessage("Họ tên không được vượt quá 100 ký tự.");

            // ===== PhoneNumber =====
            RuleFor(x => x.PhoneNumber)
                .Matches(@"^(0|\+84)[0-9]{9}$")
                .WithMessage("Số điện thoại không hợp lệ.")
                .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

            // ===== Gender =====
            RuleFor(x => x.Gender)
                .Must(g => g is "Male" or "Female" or "Other")
                .WithMessage("Giới tính phải là Male, Female hoặc Other.")
                .When(x => !string.IsNullOrWhiteSpace(x.Gender));

            // ===== DateOfBirth =====
            RuleFor(x => x.DateOfBirth)
                .Must(d =>
                {
                    if (string.IsNullOrWhiteSpace(d)) return true;
                    return DateOnly.TryParse(d, out var date)
                           && date <= DateOnly.FromDateTime(DateTime.UtcNow);
                })
                .WithMessage("Ngày sinh không hợp lệ (YYYY-MM-DD và không lớn hơn hiện tại).");

            // ===== Bio =====
            RuleFor(x => x.Bio)
                .MaximumLength(500)
                .WithMessage("Tiểu sử không được vượt quá 500 ký tự.")
                .When(x => !string.IsNullOrWhiteSpace(x.Bio));

            // ===== StudentYear =====
            RuleFor(x => x.StudentYear)
                .Must(y =>
                {
                    if (string.IsNullOrWhiteSpace(y)) return true;
                    return int.TryParse(y, out var year) && year >= 1 && year <= 10;
                })
                .WithMessage("Năm học không hợp lệ (1-10).");

            // ===== School / Faculty =====
            RuleFor(x => x.School)
                .MaximumLength(150)
                .WithMessage("Tên trường không được vượt quá 150 ký tự.")
                .When(x => !string.IsNullOrWhiteSpace(x.School));

            RuleFor(x => x.Faculty)
                .MaximumLength(150)
                .WithMessage("Tên khoa không được vượt quá 150 ký tự.")
                .When(x => !string.IsNullOrWhiteSpace(x.Faculty));

            // ===== CitizenId =====
            RuleFor(x => x.CitizenId)
                .Matches(@"^[0-9]{9,12}$")
                .WithMessage("CMND/CCCD phải là 9-12 chữ số.")
                .When(x => !string.IsNullOrWhiteSpace(x.CitizenId));

            RuleFor(x => x.CitizenIdIssuedPlace)
                .MaximumLength(150)
                .WithMessage("Nơi cấp không được vượt quá 150 ký tự.")
                .When(x => !string.IsNullOrWhiteSpace(x.CitizenIdIssuedPlace));

            // ===== Ethnicity / Religion =====
            RuleFor(x => x.Ethnicity)
                .MaximumLength(100)
                .WithMessage("Dân tộc không được vượt quá 100 ký tự.")
                .When(x => !string.IsNullOrWhiteSpace(x.Ethnicity));

            RuleFor(x => x.Religion)
                .MaximumLength(100)
                .WithMessage("Tôn giáo không được vượt quá 100 ký tự.")
                .When(x => !string.IsNullOrWhiteSpace(x.Religion));

            // ===== Address =====
            RuleFor(x => x.Province)
                .MaximumLength(100)
                .WithMessage("Tỉnh/Thành phố không được vượt quá 100 ký tự.")
                .When(x => !string.IsNullOrWhiteSpace(x.Province));

            RuleFor(x => x.District)
                .MaximumLength(100)
                .WithMessage("Quận/Huyện không được vượt quá 100 ký tự.")
                .When(x => !string.IsNullOrWhiteSpace(x.District));

            RuleFor(x => x.Ward)
                .MaximumLength(100)
                .WithMessage("Phường/Xã không được vượt quá 100 ký tự.")
                .When(x => !string.IsNullOrWhiteSpace(x.Ward));

            RuleFor(x => x.AddressLine)
                .MaximumLength(255)
                .WithMessage("Địa chỉ chi tiết không được vượt quá 255 ký tự.")
                .When(x => !string.IsNullOrWhiteSpace(x.AddressLine));

            // ===== Emergency Contact =====
            RuleFor(x => x.EmergencyContactName)
                .MaximumLength(100)
                .WithMessage("Tên người liên hệ khẩn cấp không được vượt quá 100 ký tự.")
                .When(x => !string.IsNullOrWhiteSpace(x.EmergencyContactName));

            RuleFor(x => x.EmergencyContactPhoneNumber)
                .Matches(@"^(0|\+84)[0-9]{9}$")
                .WithMessage("SĐT người liên hệ khẩn cấp không hợp lệ.")
                .When(x => !string.IsNullOrWhiteSpace(x.EmergencyContactPhoneNumber));

            RuleFor(x => x.EmergencyContactAddress)
                .MaximumLength(255)
                .WithMessage("Địa chỉ người liên hệ khẩn cấp không được vượt quá 255 ký tự.")
                .When(x => !string.IsNullOrWhiteSpace(x.EmergencyContactAddress));

            // ===== Cross-field rules =====

            // Có tên người liên hệ → phải có SĐT
            RuleFor(x => x)
                .Must(x =>
                    string.IsNullOrWhiteSpace(x.EmergencyContactName) ||
                    !string.IsNullOrWhiteSpace(x.EmergencyContactPhoneNumber))
                .WithMessage("Phải cung cấp SĐT khi có người liên hệ khẩn cấp.")
                .OverridePropertyName("EmergencyContactPhoneNumber");

            // Có CCCD → phải có nơi cấp
            RuleFor(x => x)
                .Must(x =>
                    string.IsNullOrWhiteSpace(x.CitizenId) ||
                    !string.IsNullOrWhiteSpace(x.CitizenIdIssuedPlace))
                .WithMessage("Phải cung cấp nơi cấp khi có CMND/CCCD.")
                .OverridePropertyName("CitizenIdIssuedPlace");
        }
    }

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("/api/profile/me", async (
                [FromBody] Command command,
                HttpContext httpContext,
                [FromServices] Handler handler,
                CancellationToken ct) =>
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                             ?? httpContext.User.FindFirstValue("sub")
                             ?? throw new UnauthorizedAccessException();

                var result = await handler.ExecuteAsync(userId, command, ct);
                return Results.Ok(new ApiResponse<GetMyProfile.Response>(result));
            })
            .WithTags("Profile")
            .WithName("UpdateProfile")
            .RequireAuthorization()
            .AddEndpointFilter<ValidationFilter<Command>>()
            .Produces<ApiResponse<GetMyProfile.Response>>(StatusCodes.Status200OK);
        }
    }

    public class Handler(ProfileDbContext dbContext)
    {
        public async Task<GetMyProfile.Response> ExecuteAsync(
            string userId, Command request, CancellationToken ct)
        {
            var profile = await dbContext.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId, ct);

            if (profile == null)
            {
                profile = new UserProfile { UserId = userId };
                dbContext.UserProfiles.Add(profile);
            }

            profile.FullName = request.FullName;
            profile.PhoneNumber = request.PhoneNumber;
            profile.Gender = request.Gender;
            profile.DateOfBirth = request.DateOfBirth != null
                ? DateOnly.Parse(request.DateOfBirth) : null;
            profile.Bio = request.Bio;
            profile.StudentYear = request.StudentYear;
            profile.School = request.School;
            profile.Faculty = request.Faculty;
            profile.CitizenId = request.CitizenId;
            profile.CitizenIdIssuedPlace = request.CitizenIdIssuedPlace;
            profile.Ethnicity = request.Ethnicity;
            profile.Religion = request.Religion;
            profile.Province = request.Province;
            profile.District = request.District;
            profile.Ward = request.Ward;
            profile.AddressLine = request.AddressLine;
            profile.EmergencyContactName = request.EmergencyContactName;
            profile.EmergencyContactPhoneNumber = request.EmergencyContactPhoneNumber;
            profile.EmergencyContactAddress = request.EmergencyContactAddress;
            profile.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(ct);

            return GetMyProfile.Handler.MapToResponse(profile);
        }
    }
}