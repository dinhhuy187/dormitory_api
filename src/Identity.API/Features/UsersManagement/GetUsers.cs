using Shared.Endpoints;
using FluentValidation;
using Identity.API.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Shared;

namespace Identity.API.Features.UsersManagement
{
    public static class GetUsers
    {
        public record Query(
            string? Search,
            string? Role,
            string? Status,
            int Page = 1,
            int PageSize = 20
        );
        public record Response(
            string Id,
            string Email,
            string FullName,
            bool IsActive,
            string Role
        );
        public class Validator : AbstractValidator<Query>
        {
            public Validator()
            {
                RuleFor(x => x.Page)
                    .GreaterThanOrEqualTo(1).WithMessage("Trang (Page) phải lớn hơn hoặc bằng 1.");

                RuleFor(x => x.PageSize)
                    .GreaterThan(0).WithMessage("Kích thước trang (PageSize) phải lớn hơn 0.")
                    .LessThanOrEqualTo(100).WithMessage("Kích thước trang không được vượt quá 100.");

                RuleFor(x => x.Status)
                    .Must(status => status!.ToLower() == "active" || status.ToLower() == "locked")
                    .When(x => !string.IsNullOrWhiteSpace(x.Status))
                    .WithMessage("Trạng thái chỉ được phép lọc theo 'active' hoặc 'locked'.");
            }
        }
        public class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapGet("/api/auth/users", async ([AsParameters] Query query, Handler handler, CancellationToken ct) =>
                {
                    var result = await handler.ExecuteAsync(query, ct);
                    return Results.Ok(result);
                })
                .WithTags("Users Management")
                .WithName("GetUsers")
                .RequireAuthorization(policy => policy.RequireRole("Admin"))
                .Produces<List<Response>>(StatusCodes.Status200OK);
            }
        }
        public class Handler(ApplicationDbContext dbContext)
        {
            public async Task<ApiResponse<List<Response>>> ExecuteAsync(Query request, CancellationToken cancellationToken)
            {
                var queryable = dbContext.Users.AsNoTracking().AsQueryable();

                // TÌM KIẾM (Theo Email hoặc Họ tên) ---
                if (!string.IsNullOrWhiteSpace(request.Search))
                {
                    var searchTerm = request.Search.ToLower();
                    queryable = queryable.Where(u => 
                        u.Email!.ToLower().Contains(searchTerm) || 
                        u.FullName.ToLower().Contains(searchTerm));
                }

                // TRẠNG THÁI (Active / Locked) ---
                if (!string.IsNullOrWhiteSpace(request.Status))
                {
                    bool isActiveFilter = request.Status.ToLower() == "active";
                    queryable = queryable.Where(u => u.IsActive == isActiveFilter);
                }

                // QUYỀN (Role) ---
                if (!string.IsNullOrWhiteSpace(request.Role))
                {
                    var role = await dbContext.Roles
                        .AsNoTracking()
                        .FirstOrDefaultAsync(r => r.Name == request.Role, cancellationToken);

                    if (role != null)
                    {
                        var userIdsInRole = dbContext.UserRoles
                            .Where(ur => ur.RoleId == role.Id)
                            .Select(ur => ur.UserId);

                        queryable = queryable.Where(u => userIdsInRole.Contains(u.Id));
                    }
                    else
                    {
                        queryable = queryable.Where(u => false);
                    }
                }

                var totalCount = await queryable.CountAsync(cancellationToken);

                var users = await queryable
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(u => new 
                    {
                        u.Id,
                        u.Email,
                        u.FullName,
                        u.IsActive
                    })
                    .ToListAsync(cancellationToken);

                var userIds = users.Select(u => u.Id).ToList();

                var userRoles = await dbContext.UserRoles
                    .Where(ur => userIds.Contains(ur.UserId))
                    .Join(dbContext.Roles, 
                        ur => ur.RoleId, 
                        r => r.Id, 
                        (ur, r) => new { ur.UserId, RoleName = r.Name! })
                    .ToListAsync(cancellationToken);

                var items = users.Select(u => new Response(
                    u.Id,
                    u.Email!,
                    u.FullName,
                    u.IsActive,
                    userRoles.FirstOrDefault(ur => ur.UserId == u.Id)?.RoleName ?? string.Empty
                )).ToList();

                return new ApiResponse<List<Response>>(items, new PaginationMetadata(
                    totalCount,
                    request.PageSize,
                    request.Page
                ));
            }
        }
    }
}