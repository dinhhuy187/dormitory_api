using Profile.API.Data;
using Profile.API.DTOs;
using Microsoft.EntityFrameworkCore;
using Profile.API.Models;
using Shared;

namespace Profile.API.Services;

public class ProfileService : IProfileService
{
    private readonly ProfileDbContext _db;
    private readonly IMediaService _mediaService;
    public ProfileService(ProfileDbContext db, IMediaService mediaService)
    {
        _db = db;
        _mediaService = mediaService;
    }

    public async Task<ProfileResponse> GetMyProfileAsync(string userId)
    {
        var profile = await _db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null)
        {
            profile = new UserProfile { UserId = userId };
            _db.UserProfiles.Add(profile);
            await _db.SaveChangesAsync();
        }

        return MapToResponse(profile);
    }

    public async Task<ProfileResponse> UpdateProfileAsync(string userId, UpdateProfileRequest request)
    {
        var profile = await _db.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null)
        {
            profile = new UserProfile { UserId = userId };
            _db.UserProfiles.Add(profile);
        }
        profile.FullName = request.FullName;
        profile.PhoneNumber = request.PhoneNumber;
        profile.Gender = request.Gender;
        profile.DateOfBirth = request.DateOfBirth != null
            ? DateOnly.Parse(request.DateOfBirth)
            : null;
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

        await _db.SaveChangesAsync();
        return MapToResponse(profile);
    }

    public async Task<string> UploadAvatarAsync(string userId, IFormFile file)
    {
        var userProfile = await _db.UserProfiles
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (userProfile == null)
        {
            throw new ApiException("User profile not found.", StatusCodes.Status404NotFound);
        }

        var avatarUrl = await _mediaService.UploadImageAsync(file, "dormitory_avatars");

        userProfile.AvatarUrl = avatarUrl;

        var result = await _db.SaveChangesAsync();

        if (result <= 0)
        {
            throw new ApiException("Cập nhật avatar thất bại", StatusCodes.Status400BadRequest);
        }

        return avatarUrl;
    }

    private static ProfileResponse MapToResponse(UserProfile p) => new()
    {
        Id = p.UserId,
        FullName = p.FullName,
        Email = p.Email,
        PhoneNumber = p.PhoneNumber,
        Gender = p.Gender,
        DateOfBirth = p.DateOfBirth?.ToString("yyyy-MM-dd"),
        Bio = p.Bio,
        AvatarUrl = p.AvatarUrl,
        StudentCode = p.StudentCode,
        StudentYear = p.StudentYear,
        School = p.School,
        Faculty = p.Faculty,
        CitizenId = p.CitizenId,
        CitizenIdIssuedPlace = p.CitizenIdIssuedPlace,
        Ethnicity = p.Ethnicity,
        Religion = p.Religion,
        Province = p.Province,
        District = p.District,
        Ward = p.Ward,
        AddressLine = p.AddressLine,
        EmergencyContactName = p.EmergencyContactName,
        EmergencyContactPhoneNumber = p.EmergencyContactPhoneNumber,
        EmergencyContactAddress = p.EmergencyContactAddress,
    };
}