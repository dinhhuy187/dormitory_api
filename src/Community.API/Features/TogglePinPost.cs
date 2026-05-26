using Community.API.Infrastructure.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;

namespace Community.API.Features;

public static class TogglePinPost
{
    public record Response(
        Guid PostId,
        bool IsPinned,
        DateTime UpdatedAt
    );

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/posts/{postId}/pin", async (
                Guid postId,
                [FromServices] Handler handler,
                CancellationToken ct) =>
            {
                var result = await handler.ExecuteAsync(postId, ct);
                return Results.Ok(new ApiResponse<Response>(result));
            })
            .WithTags("Posts")
            .WithName("TogglePinPost")
            .RequireAuthorization(policy => policy.RequireRole("Admin", "Staff"))
            .Produces<ApiResponse<Response>>(StatusCodes.Status200OK);
        }
    }

    public class Handler(CommunityDbContext dbContext)
    {
        public async Task<Response> ExecuteAsync(Guid postId, CancellationToken ct)
        {
            var post = await dbContext.Posts
                .FirstOrDefaultAsync(p => p.Id == postId, ct);

            if (post is null)
                throw new ApiException(
                    "Bài viết không tồn tại.",
                    StatusCodes.Status404NotFound);

            if (post.IsHidden)
                throw new ApiException(
                    "Không thể ghim bài viết đã bị ẩn.",
                    StatusCodes.Status400BadRequest);

            post.IsPinned = !post.IsPinned;
            post.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(ct);

            return new Response(
                post.Id,
                post.IsPinned,
                post.UpdatedAt
            );
        }
    }
}