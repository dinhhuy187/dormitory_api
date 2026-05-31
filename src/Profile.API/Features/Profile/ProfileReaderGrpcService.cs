using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Profile.API.Infrastructure.Database;
using Shared.Grpc.Profile;

namespace Profile.API.Features.Profile;

public sealed class ProfileReaderGrpcService(ProfileDbContext dbContext) : ProfileReader.ProfileReaderBase
{
    public override async Task<GetAvatarByUserIdResponse> GetAvatarByUserId(
        GetAvatarByUserIdRequest request,
        ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "user_id is required."));
        }

        var profile = await dbContext.UserProfiles
            .AsNoTracking()
            .Where(profile => profile.UserId == request.UserId)
            .Select(profile => new
            {
                profile.UserId,
                profile.AvatarUrl
            })
            .FirstOrDefaultAsync(context.CancellationToken);

        if (profile is null)
        {
            return new GetAvatarByUserIdResponse
            {
                UserId = request.UserId,
                AvatarUrl = string.Empty,
                ProfileExists = false
            };
        }

        return new GetAvatarByUserIdResponse
        {
            UserId = profile.UserId,
            AvatarUrl = profile.AvatarUrl ?? string.Empty,
            ProfileExists = true
        };
    }

    public override async Task<GetStudentProfilesByUserIdsResponse> GetStudentProfilesByUserIds(
        GetStudentProfilesByUserIdsRequest request,
        ServerCallContext context)
    {
        var userIds = request.UserIds
            .Where(userId => !string.IsNullOrWhiteSpace(userId))
            .Select(userId => userId.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var response = new GetStudentProfilesByUserIdsResponse();
        if (userIds.Count == 0)
        {
            return response;
        }

        var profiles = await dbContext.UserProfiles
            .AsNoTracking()
            .Where(profile => userIds.Contains(profile.UserId))
            .Select(profile => new
            {
                profile.UserId,
                profile.FullName,
                profile.StudentCode,
                profile.Email,
                profile.PhoneNumber,
                profile.Gender,
                profile.DateOfBirth,
                profile.AvatarUrl,
                profile.StudentYear,
                profile.School,
                profile.Faculty
            })
            .ToDictionaryAsync(profile => profile.UserId, StringComparer.OrdinalIgnoreCase, context.CancellationToken);

        foreach (var userId in userIds)
        {
            if (!profiles.TryGetValue(userId, out var profile))
            {
                response.Profiles.Add(new StudentProfileItem
                {
                    UserId = userId,
                    ProfileExists = false
                });
                continue;
            }

            response.Profiles.Add(new StudentProfileItem
            {
                UserId = profile.UserId,
                FullName = profile.FullName,
                StudentCode = profile.StudentCode ?? string.Empty,
                Email = profile.Email ?? string.Empty,
                PhoneNumber = profile.PhoneNumber ?? string.Empty,
                Gender = profile.Gender ?? string.Empty,
                DateOfBirth = profile.DateOfBirth?.ToString("yyyy-MM-dd") ?? string.Empty,
                AvatarUrl = profile.AvatarUrl ?? string.Empty,
                StudentYear = profile.StudentYear ?? string.Empty,
                School = profile.School ?? string.Empty,
                Faculty = profile.Faculty ?? string.Empty,
                ProfileExists = true
            });
        }

        return response;
    }
}
