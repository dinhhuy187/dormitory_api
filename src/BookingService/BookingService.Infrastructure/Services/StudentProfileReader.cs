using System.Globalization;
using BookingService.Application.Abtractions.Services;
using Grpc.Core;
using Shared;
using Shared.Grpc.Profile;

namespace BookingService.Infrastructure.Services;

public sealed class StudentProfileReader(ProfileReader.ProfileReaderClient client) : IStudentProfileReader
{
    public async Task<IReadOnlyDictionary<Guid, StudentProfileSnapshot>> GetStudentProfilesAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken)
    {
        var distinctUserIds = userIds
            .Where(userId => userId != Guid.Empty)
            .Distinct()
            .ToList();

        if (distinctUserIds.Count == 0)
        {
            return new Dictionary<Guid, StudentProfileSnapshot>();
        }

        try
        {
            var request = new GetStudentProfilesByUserIdsRequest();
            request.UserIds.AddRange(distinctUserIds.Select(userId => userId.ToString()));

            var response = await client.GetStudentProfilesByUserIdsAsync(
                request,
                deadline: DateTime.UtcNow.AddSeconds(3),
                cancellationToken: cancellationToken);

            var profiles = response.Profiles
                .Select(MapProfile)
                .Where(profile => profile is not null)
                .Cast<StudentProfileSnapshot>()
                .GroupBy(profile => profile.UserId)
                .ToDictionary(group => group.Key, group => group.First());

            foreach (var userId in distinctUserIds)
            {
                profiles.TryAdd(userId, MissingProfile(userId));
            }

            return profiles;
        }
        catch (RpcException ex) when (ex.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded)
        {
            throw new ApiException("ProfileService is unavailable. Please try again later.", 503);
        }
        catch (RpcException)
        {
            throw new ApiException("ProfileService request failed.", 502);
        }
    }

    private static StudentProfileSnapshot? MapProfile(StudentProfileItem item)
    {
        if (!Guid.TryParse(item.UserId, out var userId))
        {
            return null;
        }

        if (!item.ProfileExists)
        {
            return MissingProfile(userId);
        }

        return new StudentProfileSnapshot(
            userId,
            EmptyToNull(item.FullName),
            EmptyToNull(item.StudentCode),
            EmptyToNull(item.Email),
            EmptyToNull(item.PhoneNumber),
            EmptyToNull(item.Gender),
            ParseDateOnly(item.DateOfBirth),
            EmptyToNull(item.AvatarUrl),
            EmptyToNull(item.StudentYear),
            EmptyToNull(item.School),
            EmptyToNull(item.Faculty),
            true);
    }

    private static StudentProfileSnapshot MissingProfile(Guid userId)
    {
        return new StudentProfileSnapshot(
            userId,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            false);
    }

    private static string? EmptyToNull(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static DateOnly? ParseDateOnly(string value)
    {
        return DateOnly.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date)
            ? date
            : null;
    }
}
