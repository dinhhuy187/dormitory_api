using System.Net.Http.Json;
using Bogus;
using Microsoft.EntityFrameworkCore;
using Profile.API.Domain.Entities;

namespace Profile.API.Infrastructure.Database;

public static class SeedData
{
    private const string DefaultStudentYear = "2023";
    private const string DefaultAvatarUrl = "https://khoanhdep.com/wp-content/uploads/2026/02/meme-do-mixi-10.jpg";
    private const int BatchSize = 1000;

    private static readonly string[] Schools =
    [
        "Trường Đại học Công nghệ Thông tin",
        "Trường Đại học Bách khoa",
        "Trường Đại học Khoa học Tự nhiên",
        "Trường Đại học Kinh tế",
        "Trường Đại học Sư phạm Kỹ thuật"
    ];

    private static readonly string[] Faculties =
    [
        "Công nghệ thông tin",
        "Hệ thống thông tin",
        "Kỹ thuật phần mềm",
        "Khoa học máy tính",
        "Mạng máy tính và truyền thông",
        "Kế toán",
        "Quản trị kinh doanh"
    ];

    private static readonly string[] Ethnicities =
    [
        "Kinh",
        "Tày",
        "Thái",
        "Mường",
        "Hoa",
        "Khmer"
    ];

    private static readonly string[] Religions =
    [
        "Không",
        "Phật giáo",
        "Công giáo",
        "Tin Lành"
    ];

    public static async Task<SeedResult> SeedAsync(
        ProfileDbContext dbContext,
        IHttpClientFactory httpClientFactory,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var students = await GetStudentsFromIdentityAsync(httpClientFactory, cancellationToken);
        if (students.Count == 0)
        {
            logger.LogWarning("IdentityService student sync returned no students. Profile seed skipped.");
            return new SeedResult(0, 0, 0);
        }

        var validStudents = students
            .Where(student => !string.IsNullOrWhiteSpace(student.Id))
            .GroupBy(student => student.Id.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(student => student.UserName)
            .ToList();

        var created = 0;
        var updated = 0;
        var skipped = students.Count - validStudents.Count;

        foreach (var batch in validStudents.Chunk(BatchSize))
        {
            var userIds = batch
                .Select(student => student.Id.Trim())
                .ToList();

            var existingProfiles = await dbContext.UserProfiles
                .Where(profile => userIds.Contains(profile.UserId))
                .ToDictionaryAsync(profile => profile.UserId, StringComparer.OrdinalIgnoreCase, cancellationToken);

            foreach (var student in batch)
            {
                var userId = student.Id.Trim();
                if (existingProfiles.TryGetValue(userId, out var profile))
                {
                    if (ApplyIdentitySnapshot(profile, student))
                    {
                        updated++;
                    }

                    continue;
                }

                dbContext.UserProfiles.Add(CreateProfile(student));
                created++;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation(
            "Profile seed completed. Created: {CreatedCount}, Updated: {UpdatedCount}, Skipped: {SkippedCount}.",
            created,
            updated,
            skipped);

        return new SeedResult(created, updated, skipped);
    }

    private static async Task<IReadOnlyList<IdentityStudentSyncDto>> GetStudentsFromIdentityAsync(
        IHttpClientFactory httpClientFactory,
        CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("IdentityServiceClient");
        var students = await client.GetFromJsonAsync<List<IdentityStudentSyncDto>>(
            "/api/auth/users/students/sync",
            cancellationToken);

        return students ?? [];
    }

    private static UserProfile CreateProfile(IdentityStudentSyncDto student)
    {
        var faker = CreateFaker(student.Id);
        var gender = faker.PickRandom("Male", "Female");
        var province = faker.Address.State();
        var district = faker.Address.City();
        var ward = $"Phường {faker.Random.Int(1, 20)}";
        var addressLine = $"{faker.Random.Int(1, 250)} {faker.Address.StreetName()}";

        return new UserProfile
        {
            UserId = student.Id.Trim(),
            FullName = ResolveFullName(student),
            Email = ResolveEmail(student),
            PhoneNumber = GenerateVietnamesePhoneNumber(faker),
            Gender = gender,
            DateOfBirth = DateOnly.FromDateTime(faker.Date.Between(
                new DateTime(2001, 1, 1),
                new DateTime(2007, 12, 31))),
            Bio = faker.Lorem.Sentence(12),
            AvatarUrl = DefaultAvatarUrl,
            StudentCode = ResolveStudentCode(student),
            StudentYear = DefaultStudentYear,
            School = faker.PickRandom(Schools),
            Faculty = faker.PickRandom(Faculties),
            CitizenId = faker.Random.ReplaceNumbers("############"),
            CitizenIdIssuedPlace = province,
            Ethnicity = faker.PickRandom(Ethnicities),
            Religion = faker.PickRandom(Religions),
            Province = province,
            District = district,
            Ward = ward,
            AddressLine = addressLine,
            EmergencyContactName = faker.Name.FullName(),
            EmergencyContactPhoneNumber = GenerateVietnamesePhoneNumber(faker),
            EmergencyContactAddress = $"{addressLine}, {ward}, {district}, {province}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static bool ApplyIdentitySnapshot(UserProfile profile, IdentityStudentSyncDto student)
    {
        var changed = false;
        var fullName = ResolveFullName(student);
        var email = ResolveEmail(student);
        var studentCode = ResolveStudentCode(student);

        if (profile.FullName != fullName)
        {
            profile.FullName = fullName;
            changed = true;
        }

        if (profile.Email != email)
        {
            profile.Email = email;
            changed = true;
        }

        if (profile.StudentCode != studentCode)
        {
            profile.StudentCode = studentCode;
            changed = true;
        }

        if (profile.StudentYear != DefaultStudentYear)
        {
            profile.StudentYear = DefaultStudentYear;
            changed = true;
        }

        if (profile.AvatarUrl != DefaultAvatarUrl)
        {
            profile.AvatarUrl = DefaultAvatarUrl;
            changed = true;
        }

        if (changed)
        {
            profile.UpdatedAt = DateTime.UtcNow;
        }

        return changed;
    }

    private static Faker CreateFaker(string seedValue)
    {
        var seed = StringComparer.OrdinalIgnoreCase.GetHashCode(seedValue);
        return new Faker("vi")
        {
            Random = new Randomizer(seed)
        };
    }

    private static string ResolveFullName(IdentityStudentSyncDto student)
    {
        if (!string.IsNullOrWhiteSpace(student.FullName))
        {
            return student.FullName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(student.UserName))
        {
            return $"Sinh viên {student.UserName.Trim()}";
        }

        return "Sinh viên";
    }

    private static string? ResolveEmail(IdentityStudentSyncDto student)
    {
        if (!string.IsNullOrWhiteSpace(student.Email))
        {
            return student.Email.Trim();
        }

        var studentCode = ResolveStudentCode(student);
        return string.IsNullOrWhiteSpace(studentCode)
            ? null
            : $"{studentCode}@student.edu.vn";
    }

    private static string? ResolveStudentCode(IdentityStudentSyncDto student)
    {
        return string.IsNullOrWhiteSpace(student.UserName)
            ? null
            : student.UserName.Trim();
    }

    private static string GenerateVietnamesePhoneNumber(Faker faker)
    {
        return $"0{faker.Random.ReplaceNumbers("#########")}";
    }

    private sealed record IdentityStudentSyncDto(
        string Id,
        string UserName,
        string Email,
        string FullName);

    public sealed record SeedResult(int CreatedCount, int UpdatedCount, int SkippedCount);
}
