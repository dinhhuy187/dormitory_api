using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Profile.API.Domain.Entities;
using Profile.API.Infrastructure.Database;
using Shared;
using Shared.Endpoints;
using System.Security.Claims;

namespace Profile.API.Features.Profile;

public static class GetMyProfile
{
    public record Response(
        string Id,
        string FullName,
        string? Email,
        string? PhoneNumber,
        string? Gender,
        string? DateOfBirth,
        string? Bio,
        string? AvatarUrl,
        string? StudentCode,
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

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/profile/me", async (
                HttpContext httpContext,
                [FromServices] Handler handler,
                CancellationToken ct) =>
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? httpContext.User.FindFirstValue("sub")
                ?? throw new UnauthorizedAccessException();

                var result = await handler.ExecuteAsync(userId, ct);
                return Results.Ok(new ApiResponse<Response>(result));
            })
            .WithTags("Profile")
            .WithName("GetMyProfile")
            .RequireAuthorization()
            .Produces<ApiResponse<Response>>(StatusCodes.Status200OK); ;
        }
    }

    public class Handler(ProfileDbContext dbContext)
    {
        public async Task<Response> ExecuteAsync(string userId, CancellationToken ct)
        {
            var profile = await dbContext.UserProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId, ct);

            if (profile == null)
            {
                profile = new UserProfile { UserId = userId };
                dbContext.UserProfiles.Add(profile);
                await dbContext.SaveChangesAsync(ct);
            }

            return MapToResponse(profile);
        }

        internal static Response MapToResponse(UserProfile p) => new(
            p.UserId, p.FullName, p.Email, p.PhoneNumber,
            p.Gender, p.DateOfBirth?.ToString("yyyy-MM-dd"), p.Bio, p.AvatarUrl,
            p.StudentCode, p.StudentYear, p.School, p.Faculty,
            p.CitizenId, p.CitizenIdIssuedPlace, p.Ethnicity, p.Religion,
            p.Province, p.District, p.Ward, p.AddressLine,
            p.EmergencyContactName, p.EmergencyContactPhoneNumber, p.EmergencyContactAddress
        );
    }
}
