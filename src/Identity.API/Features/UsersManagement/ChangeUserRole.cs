using Shared.Endpoints;
using FluentValidation;
using Identity.API.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shared;

namespace Identity.API.Features.UsersManagement
{
    public static class ChangeUserRole
    {
        public record RequestBody(string Role);
        public record Command(string UserId, string Role);
        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.UserId)
                    .NotEmpty().WithMessage("ID người dùng không được để trống.");

                RuleFor(x => x.Role)
                    .NotEmpty().WithMessage("Tên quyền (Role) không được để trống.")
                    .Must(role => new[] { "Admin", "Manager", "SeniorManager", "Student" }.Contains(role))
                    .WithMessage("Tên Role không hợp lệ. Phải là một trong các giá trị: Admin, Manager, SeniorManager, Student.");
            }
        }
        public class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapPut("/api/auth/users/{id}/role", async (string id, [FromBody] RequestBody body, Handler handler, CancellationToken ct) =>
                {
                    var command = new Command(id, body.Role);
                    await handler.ExecuteAsync(command, ct);
                    
                    return Results.NoContent();
                })
                .WithTags("Users Management")
                .WithName("ChangeUserRole")
                .RequireAuthorization(policy => policy.RequireRole("Admin"))
                .Produces(StatusCodes.Status204NoContent);
            }
        }
        public class Handler(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            public async Task<bool> ExecuteAsync(Command request, CancellationToken cancellationToken)
            {
                var user = await userManager.FindByIdAsync(request.UserId);
                if (user == null)
                {
                    throw new ApiException("Không tìm thấy người dùng trong hệ thống.", 404);
                }

                var roleExists = await roleManager.RoleExistsAsync(request.Role);
                if (!roleExists)
                {
                    throw new ApiException("Quyền không tồn tại. Vui lòng kiểm tra lại.", 400);
                }

                var currentRoles = await userManager.GetRolesAsync(user);
                
                if (currentRoles.Contains(request.Role) && currentRoles.Count == 1)
                {
                    return true;
                }

                if (currentRoles.Any())
                {
                    var removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
                    if (!removeResult.Succeeded)
                    {
                        throw new ApiException("Lỗi khi gỡ bỏ quyền cũ của người dùng.", 500);
                    }
                }

                var addResult = await userManager.AddToRoleAsync(user, request.Role);
                if (!addResult.Succeeded)
                {
                    throw new ApiException("Lỗi khi gán quyền mới cho người dùng.", 500);
                }

                return true;
            }
        }
    }
}