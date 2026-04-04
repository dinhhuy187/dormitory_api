using Shared.Endpoints;
using FluentValidation;
using Identity.API.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Shared;

namespace Identity.API.Features.UsersManagement
{
    public static class GetUserById
    {
        public record Query(string Id);
        public record Response(
            string Id,
            string Email,
            string FullName,
            bool IsActive,
            string Role,
            string AvatarUrl,
            DateTime JoinedAt,
            DateTime LastActiveAt
        );
        public class Validator : AbstractValidator<Query>
        {
            public Validator()
            {
                RuleFor(x => x.Id)
                    .NotEmpty().WithMessage("ID người dùng không được để trống.");
            }
        }
        public class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapGet("/api/auth/users/{id}", async (string id, Handler handler, CancellationToken ct) =>
                {
                    var result = await handler.ExecuteAsync(new Query(id), ct);
                    return Results.Ok(result);
                })
                .WithTags("Users Management")
                .WithName("GetUserById")
                .RequireAuthorization(policy => policy.RequireRole("Admin"))
                .AddEndpointFilter<ValidationFilter<Query>>()
                .Produces<Response>(StatusCodes.Status200OK);
            }
        }
        public class Handler(ApplicationDbContext dbContext)
        {
            public async Task<ApiResponse<Response>> ExecuteAsync(Query request, CancellationToken cancellationToken)
            {
                // Truy vấn thông tin cơ bản của User
                var user = await dbContext.Users
                    .AsNoTracking()
                    .Where(u => u.Id == request.Id)
                    .Select(u => new 
                    {
                        u.Id,
                        u.Email,
                        u.FullName,
                        u.IsActive,
                        u.CreatedAt
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (user == null)
                {
                    throw new ApiException("Không tìm thấy người dùng trong hệ thống.", 404);
                }

                // Truy vấn Role duy nhất của User
                var roleName = await dbContext.UserRoles
                    .Where(ur => ur.UserId == user.Id)
                    .Join(dbContext.Roles, 
                          ur => ur.RoleId, 
                          r => r.Id, 
                          (ur, r) => r.Name)
                    .FirstOrDefaultAsync(cancellationToken);

                var response = new Response(
                    user.Id,
                    user.Email!,
                    user.FullName,
                    user.IsActive,
                    roleName ?? string.Empty,
                    "https://upload.wikimedia.org/wikipedia/commons/thumb/0/09/DoMixi1989.jpg/960px-DoMixi1989.jpg",
                    user.CreatedAt,
                    DateTime.UtcNow - TimeSpan.FromHours(2) // Placeholder cho LastActiveAt vì chưa có tracking thực tế
                );

                return new ApiResponse<Response>(response);
            }
        }
    }
}