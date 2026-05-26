using Community.API.Domain.Enums;
using Community.API.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Endpoints;
using Shared.Services;
using System.Security.Claims;

namespace Community.API.Features;

public static class GetHiddenPosts
{
    public record Query(
        string? Cursor,
        string? PostType,
        int PageSize = 20
    );

    public record PostDto(
        Guid Id,
        string AuthorId,
        string AuthorName,
        string? AvatarUrl,
        string Content,
        List<string> MediaUrls,
        string PostType,
        bool IsPinned,
        bool IsHidden,
        int LikeCount,
        int CommentCount,
        bool IsLikedByMe,
        DateTime CreatedAt
    );

    public record Response(
        List<PostDto> Items,
        string? NextCursor,
        bool HasMore
    );

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/community/posts/hidden", async (
                [AsParameters] Query query,
                HttpContext httpContext,
                Handler handler,
                CancellationToken ct) =>
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? httpContext.User.FindFirstValue("sub")
                    ?? throw new UnauthorizedAccessException();

                // Lấy danh sách roles của user từ Token (hỗ trợ cả claim 'role' thông thường và 'ClaimsIdentity.RoleClaimType')
                var roles = httpContext.User.FindAll(ClaimTypes.Role)
                    .Select(c => c.Value)
                    .Concat(httpContext.User.FindAll("role").Select(c => c.Value))
                    .ToList();

                var accessToken = httpContext.Request.Headers.Authorization
                    .ToString()
                    .Replace("Bearer ", string.Empty);

                var result = await handler.ExecuteAsync(
                    query,
                    userId,
                    roles,
                    accessToken,
                    ct);

                return Results.Ok(new ApiResponse<Response>(result));
            })
            .WithTags("Posts")
            .WithName("GetHiddenPosts")
            .RequireAuthorization()
            .Produces<Response>(StatusCodes.Status200OK);
        }
    }

    public class Handler(
        CommunityDbContext dbContext,
        IProfileService profileService)
    {
        public async Task<Response> ExecuteAsync(
            Query request,
            string userId,
            List<string> roles,
            string accessToken,
            CancellationToken ct)
        {
            // 1. Validate dữ liệu đầu vào
            if (request.PageSize <= 0 || request.PageSize > 50)
            {
                throw new ApiException(
                    "PageSize phải từ 1 đến 50.",
                    StatusCodes.Status400BadRequest);
            }

            if (!string.IsNullOrEmpty(request.PostType) &&
                !Enum.TryParse<PostType>(request.PostType, ignoreCase: true, out _))
            {
                throw new ApiException(
                    $"PostType không hợp lệ. Giá trị hợp lệ: {string.Join(", ", Enum.GetNames<PostType>())}",
                    StatusCodes.Status400BadRequest);
            }

            // 2. Khởi tạo query cơ bản (Chỉ lấy các bài viết bị ẩn)
            var query = dbContext.Posts
                .AsNoTracking()
                .Where(p => p.IsHidden)
                .AsQueryable();

            // 3. PHÂN QUYỀN: Nếu KHÔNG phải Admin, chỉ cho phép xem bài ẩn của chính mình
            bool isAdmin = roles.Contains("Admin", StringComparer.OrdinalIgnoreCase);
            if (!isAdmin)
            {
                query = query.Where(p => p.AuthorId == userId);
            }

            // 4. Lọc theo PostType nếu có
            if (!string.IsNullOrEmpty(request.PostType) &&
                Enum.TryParse<PostType>(request.PostType, ignoreCase: true, out var parsedType))
            {
                query = query.Where(p => p.PostType == parsedType);
            }

            // 5. Áp dụng Cursor Pagination
            if (!string.IsNullOrEmpty(request.Cursor) &&
                DateTime.TryParse(request.Cursor, out var cursorTime))
            {
                query = query.Where(p => p.CreatedAt < cursorTime);
            }

            // 6. Lấy dữ liệu raw từ Database
            var itemsRaw = await query
                .OrderByDescending(p => p.CreatedAt)
                .Take(request.PageSize + 1)
                .Select(p => new
                {
                    p.Id,
                    p.AuthorId,
                    p.Content,
                    p.MediaUrls,
                    p.PostType,
                    p.IsPinned,
                    p.IsHidden,
                    p.LikeCount,
                    p.CommentCount,
                    p.CreatedAt
                })
                .ToListAsync(ct);

            // 7. Xử lý HasMore và NextCursor
            var hasMore = itemsRaw.Count > request.PageSize;
            if (hasMore)
            {
                itemsRaw.RemoveAt(itemsRaw.Count - 1);
            }

            string? nextCursor = hasMore
                ? itemsRaw.Last().CreatedAt.ToString("o")
                : null;

            // 8. Lấy thông tin Profile của các Author
            var authorIds = itemsRaw.Select(x => x.AuthorId).Distinct();
            var profiles = await profileService.GetProfilesAsync(authorIds, accessToken, ct);

            // 9. Kiểm tra xem User hiện tại đã thích những bài viết này chưa
            var allPostIds = itemsRaw.Select(x => x.Id).ToList();
            var likedPostIds = await dbContext.PostLikes
                .AsNoTracking()
                .Where(l => l.UserId == userId && allPostIds.Contains(l.PostId))
                .Select(l => l.PostId)
                .ToListAsync(ct);

            var likedSet = likedPostIds.ToHashSet();

            // 10. Map sang Dto kết quả
            var items = itemsRaw.Select(p =>
            {
                profiles.TryGetValue(p.AuthorId, out var profile);

                return new PostDto(
                    p.Id,
                    p.AuthorId,
                    profile?.FullName ?? "Người dùng",
                    profile?.AvatarUrl,
                    p.Content,
                    p.MediaUrls,
                    p.PostType.ToString(),
                    p.IsPinned,
                    p.IsHidden,
                    p.LikeCount,
                    p.CommentCount,
                    likedSet.Contains(p.Id),
                    p.CreatedAt
                );
            }).ToList();

            return new Response(items, nextCursor, hasMore);
        }
    }
}