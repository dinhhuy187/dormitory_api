using Community.API.Domain.Entities;
using Community.API.Domain.Enums;
using Community.API.Infrastructure.Database;
using Microsoft.AspNetCore.Mvc;
using Shared;
using Shared.Endpoints;
using System.Security.Claims;

namespace Community.API.Features;

public static class CreatePost
{
    public record Response(
        Guid Id,
        string AuthorId,
        string Content,
        List<string> MediaUrls,
        string PostType,
        bool IsPinned,
        bool IsHidden,
        DateTime CreatedAt
    );

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/community/posts", async (
                HttpContext httpContext,
                [FromForm] string content,
                [FromForm] PostType postType,
                IFormFileCollection? files,
                [FromServices] Handler handler,
                CancellationToken ct) =>
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? httpContext.User.FindFirstValue("sub")
                    ?? throw new UnauthorizedAccessException();

                if (string.IsNullOrWhiteSpace(content) || content.Length < 10)
                {
                    return Results.BadRequest(
                        new ApiResponse<string>(
                            "Nội dung phải có ít nhất 10 ký tự."));
                }

                if (content.Length > 5000)
                {
                    return Results.BadRequest(
                        new ApiResponse<string>(
                            "Nội dung không được vượt quá 5000 ký tự."));
                }

                if (!Enum.IsDefined(postType))
                {
                    return Results.BadRequest(
                        new ApiResponse<string>(
                            $"PostType không hợp lệ. Giá trị hợp lệ: {string.Join(", ", Enum.GetNames<PostType>())}"));
                }

                // Không cho tạo Announcement từ endpoint này
                if (postType == PostType.Announcement)
                {
                    return Results.Forbid();
                }

                if (files is not null && files.Count > 10)
                {
                    return Results.BadRequest(
                        new ApiResponse<string>(
                            "Tối đa 10 file media."));
                }

                var result = await handler.ExecuteAsync(
                    userId,
                    content,
                    postType,
                    files,
                    ct);

                return Results.Created(
                    $"/api/community/posts/{result.Id}",
                    new ApiResponse<Response>(result));
            })
            .WithTags("Posts")
            .WithName("CreatePost")
            .RequireAuthorization()
            .DisableAntiforgery()
            .Produces<Response>(StatusCodes.Status201Created);
        }
    }

    public class Handler(
        CommunityDbContext dbContext,
        IMediaService mediaService)
    {
        public async Task<Response> ExecuteAsync(
            string userId,
            string content,
            PostType postType,
            IFormFileCollection? files,
            CancellationToken ct)
        {
            var mediaUrls = await UploadMediaAsync(
                files,
                mediaService);

            var post = new Post
            {
                AuthorId = userId,
                Content = content,
                PostType = postType,
                IsPinned = false,
                MediaUrls = mediaUrls,
            };

            dbContext.Posts.Add(post);

            await dbContext.SaveChangesAsync(ct);

            return new Response(
                post.Id,
                post.AuthorId,
                post.Content,
                post.MediaUrls,
                post.PostType.ToString(),
                post.IsPinned,
                post.IsHidden,
                post.CreatedAt);
        }
    }

    private static async Task<List<string>> UploadMediaAsync(
        IFormFileCollection? files,
        IMediaService mediaService)
    {
        var mediaUrls = new List<string>();

        if (files is null || files.Count == 0)
        {
            return mediaUrls;
        }

        var allowedExtensions = new[]
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp",
            ".mp4"
        };

        foreach (var file in files)
        {
            var extension = Path
                .GetExtension(file.FileName)
                .ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                throw new ApiException(
                    $"File '{file.FileName}' có định dạng không hỗ trợ.",
                    StatusCodes.Status400BadRequest);
            }

            if (file.Length > 10 * 1024 * 1024)
            {
                throw new ApiException(
                    $"File '{file.FileName}' quá lớn (tối đa 10MB).",
                    StatusCodes.Status400BadRequest);
            }

            var url = await mediaService.UploadImageAsync(
                file,
                "dormitory_posts");

            mediaUrls.Add(url);
        }

        return mediaUrls;
    }
}