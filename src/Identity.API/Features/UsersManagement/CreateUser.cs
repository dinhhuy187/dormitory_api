using FluentValidation;
using Identity.API.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shared;
using Shared.Endpoints;

namespace Identity.API.Features.UsersManagement;
public static class CreateUser
{
    public record Command(string Email, string Password, string FullName, string Role);
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Email).NotEmpty().WithMessage("Email không được để trống")
                .EmailAddress().WithMessage("Email không hợp lệ");
            RuleFor(x => x.Password).NotEmpty().WithMessage("Mật khẩu không được để trống")
                .MinimumLength(6).WithMessage("Mật khẩu phải có ít nhất 6 ký tự");
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Họ và tên không được để trống")
                .MaximumLength(100).WithMessage("Họ và tên không được vượt quá 100 ký tự");
            RuleFor(x => x.Role)
                .NotEmpty().WithMessage("Role không được để trống")
                .Must(role => new[] { "Admin", "User" }.Contains(role))
                .WithMessage("Role phải là 'Admin' hoặc 'User'");
        }
        public class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapPost("/api/auth/users", async ([FromBody] Command command, Handler handler, CancellationToken ct) =>
                {
                    await handler.ExecuteAsync(command, ct);
                    return Results.NoContent();
                })
                .WithTags("Users Management")
                .WithName("CreateUser")
                .RequireAuthorization(policy => policy.RequireRole("Admin"))
                .AddEndpointFilter<ValidationFilter<Command>>()
                .Produces(StatusCodes.Status204NoContent);
            }
        }
        public class Handler(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            public async Task<bool> ExecuteAsync(Command request, CancellationToken cancellationToken)
            {
                var existingUser = await userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    throw new ApiException("Email already in use", 400);
                }

                var roleExists = await roleManager.RoleExistsAsync(request.Role);
                if (!roleExists)
                {
                    throw new ApiException("Role does not exist. Please check again.", 400);
                }

                var newUser = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    FullName = request.FullName,
                    IsActive = true,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(newUser, request.Password);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                    throw new ApiException($"Failed to create user: {errors}", 500);
                }

                var addToRoleResult = await userManager.AddToRoleAsync(newUser, request.Role);
                if (!addToRoleResult.Succeeded)
                {
                    await userManager.DeleteAsync(newUser);
                    var errors = string.Join("; ", addToRoleResult.Errors.Select(e => e.Description));
                    throw new ApiException($"Failed to assign role to user: {errors}", 500);
                }

                return true;
            }
        }
    }        
}
