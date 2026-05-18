using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Profile.API.Infrastructure.Database;
using Shared;
using Shared.Endpoints;

namespace Profile.API.Features;

public static class GetProfilesBatch
{
    public record Request(List<string> UserIds);

    public record Response(
        string UserId,
        string FullName,
        string? AvatarUrl
    );

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/profile/batch", async (
                [FromBody] Request request,
                [FromServices] Handler handler,
                CancellationToken ct) =>
            {
                var result = await handler.ExecuteAsync(request, ct);
                return Results.Ok(result);
            })
            .WithTags("Profile")
            .WithName("GetProfilesBatch")
            .RequireAuthorization()
            .Produces<List<Response>>(StatusCodes.Status200OK);
        }
    }

    public class Handler(ProfileDbContext dbContext)
    {
        public async Task<List<Response>> ExecuteAsync(Request req, CancellationToken ct)
        {
            if (req.UserIds is null || req.UserIds.Count == 0)
                return [];

            return await dbContext.UserProfiles
                .AsNoTracking()
                .Where(p => req.UserIds.Contains(p.UserId))
                .Select(p => new Response(p.UserId, p.FullName, p.AvatarUrl))
                .ToListAsync(ct);
        }
    }
}