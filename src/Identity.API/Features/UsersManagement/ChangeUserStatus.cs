using Shared.Endpoints;
using FluentValidation;
using Identity.API.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shared;

namespace Identity.API.Features.UsersManagement
{
    public static class ChangeUserStatus
    {
        public record RequestBody(string Status);
        public record Command(string UserId, string Status);

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.UserId)
                    .NotEmpty().WithMessage("ID người dùng không được để trống.");

                RuleFor(x => x.Status)
                    .Cascade(CascadeMode.Stop)
                    .NotEmpty().WithMessage("Trạng thái không được để trống.")
                    .Must(status => status.ToLower() == "active" || status.ToLower() == "locked")
                    .WithMessage("Trạng thái chỉ được nhận giá trị 'active' hoặc 'locked'.");
            }
        }
        public class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapPut("/api/auth/users/{id}/status", async (string id, [FromBody] RequestBody body, Handler handler, CancellationToken ct) =>
                {
                    var command = new Command(id, body.Status);
                    await handler.ExecuteAsync(command, ct);
                    
                    return Results.NoContent();
                })
                .WithTags("Users Management")
                .WithName("ChangeUserStatus")
                .RequireAuthorization(policy => policy.RequireRole("Admin"))
                .Produces(StatusCodes.Status204NoContent);
            }
        }
        public class Handler(
            UserManager<ApplicationUser> userManager)
        {
            public async Task<bool> ExecuteAsync(Command request, CancellationToken cancellationToken)
            {
                var user = await userManager.FindByIdAsync(request.UserId);
                if (user == null)
                {
                    throw new ApiException("Không tìm thấy người dùng trong hệ thống.", 404);
                }

                bool isActivating = request.Status.ToLower() == "active";

                if (user.IsActive == isActivating)
                {
                    return true;
                }

                user.IsActive = isActivating;

                // [Thao tác nghiệp vụ chuẩn của Identity] 
                // Đồng bộ cờ IsActive với cơ chế Lockout mặc định của ASP.NET Identity
                if (isActivating)
                {
                    // Mở khóa: Đặt thời hạn khóa về null
                    await userManager.SetLockoutEndDateAsync(user, null);
                }
                else
                {
                    // Khóa tài khoản: Đặt thời hạn khóa đến mức tối đa (vĩnh viễn)
                    await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                }

                // Lưu thay đổi cờ IsActive vào Database
                var result = await userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    throw new ApiException("Lỗi khi cập nhật trạng thái người dùng.", 500);
                }

                return true;
            }
        }
    }
}