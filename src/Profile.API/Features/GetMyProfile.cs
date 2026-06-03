using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Profile.API.Domain.Entities;
using Profile.API.Infrastructure.Database;
using Shared;
using Shared.Endpoints;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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
        string? EmergencyContactAddress,
        RoomResponse? Room
    );

    public record RoomResponse(
        string Id,
        string Name,
        string Building);

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

                var accessToken = GetBearerToken(httpContext);
                var result = await handler.ExecuteAsync(userId, accessToken, ct);
                return Results.Ok(new ApiResponse<Response>(result));
            })
            .WithTags("Profile")
            .WithName("GetMyProfile")
            .RequireAuthorization()
            .Produces<Response>(StatusCodes.Status200OK); ;
        }

        private static string? GetBearerToken(HttpContext httpContext)
        {
            var authorization = httpContext.Request.Headers.Authorization.ToString();
            const string bearerPrefix = "Bearer ";

            return !string.IsNullOrWhiteSpace(authorization) &&
                   authorization.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase)
                ? authorization[bearerPrefix.Length..].Trim()
                : null;
        }
    }

    public class Handler(ProfileDbContext dbContext, IHttpClientFactory httpClientFactory, ILogger<Handler> logger)
    {
        public async Task<Response> ExecuteAsync(string userId, string? accessToken, CancellationToken ct)
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

            var room = await GetCurrentRoomAsync(accessToken, ct);
            return MapToResponse(profile, room);
        }

        internal static Response MapToResponse(UserProfile p, RoomResponse? room = null) => new(
            p.UserId, p.FullName, p.Email, p.PhoneNumber,
            p.Gender, p.DateOfBirth?.ToString("yyyy-MM-dd"), p.Bio, p.AvatarUrl,
            p.StudentCode, p.StudentYear, p.School, p.Faculty,
            p.CitizenId, p.CitizenIdIssuedPlace, p.Ethnicity, p.Religion,
            p.Province, p.District, p.Ward, p.AddressLine,
            p.EmergencyContactName, p.EmergencyContactPhoneNumber, p.EmergencyContactAddress,
            room
        );

        private async Task<RoomResponse?> GetCurrentRoomAsync(string? accessToken, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return null;
            }

            try
            {
                var client = httpClientFactory.CreateClient("BookingServiceClient");
                using var request = new HttpRequestMessage(HttpMethod.Get, "/api/bookings/me/current-room");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                using var response = await client.SendAsync(request, ct);
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning(
                        "BookingService returned {StatusCode} while loading current room for profile.",
                        (int)response.StatusCode);
                    return null;
                }

                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<CurrentRoomDto?>>(cancellationToken: ct);
                var room = apiResponse?.Data;
                return room is null
                    ? null
                    : new RoomResponse(room.Id, room.Name, room.Building);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not load current room for profile.");
                return null;
            }
        }

        private sealed record CurrentRoomDto(string Id, string Name, string Building);
    }
}
