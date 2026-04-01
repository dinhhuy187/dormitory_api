using Carter;
using FluentValidation;
using Identity.API.Infrastructure.Database;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared;

namespace Identity.API.Features.UsersManagement
{
    public static class GetUserById
    {
        public record Query(string Id) : IRequest<ApiResponse<Response>>;
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
                RuleFor(x => x.Id)
                    .NotEmpty().WithMessage("ID người dùng không được để trống.");
            }
        }
        public class Endpoint : ICarterModule
        {
            public void AddRoutes(IEndpointRouteBuilder app)
            {
                app.MapGet("/api/auth/users/{id}", async (string id, IMediator mediator) =>
                {
                    var result = await mediator.Send(new Query(id));
                    return Results.Ok(result);
                })
                .WithTags("Users Management")
                .WithName("GetUserById")
                .RequireAuthorization(policy => policy.RequireRole("Admin"))
                .Produces<Response>(StatusCodes.Status200OK);
            }
        }
        public class Handler(ApplicationDbContext dbContext) : IRequestHandler<Query, ApiResponse<Response>>
        {
            public async Task<ApiResponse<Response>> Handle(Query request, CancellationToken cancellationToken)
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
                        u.IsActive
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
                    roleName ?? string.Empty
                );

                return new ApiResponse<Response>(response);
            }
        }
    }
}