using Shared.Endpoints;
using FluentValidation;
using Identity.API.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shared;

namespace Identity.API.Features.Auth
{
    public static class Register
    {
        public record Command(string Email,string FullName, string Password);
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
            }
        }
        public class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapPost("/api/auth/register", async ([FromBody] Command command, Handler handler, CancellationToken ct) =>
                {
                    await handler.ExecuteAsync(command, ct);
                    return Results.NoContent();
                })
                .WithTags("Auth")
                .WithName("Register")
                .AddEndpointFilter<ValidationFilter<Command>>()
                .Produces(StatusCodes.Status204NoContent);
            }
        }
        public class Handler(
            UserManager<ApplicationUser> userManager)
        {
            public async Task<bool> ExecuteAsync(Command request, CancellationToken cancellationToken)
            {
                var existingUser = await userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    throw new ApiException("Email already in use", 400);
                }

                var newUser = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    FullName = request.FullName,
                    IsActive = true,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(newUser, request.Password);
                
                if (!result.Succeeded) 
                {
                    var errorMessages = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new ApiException($"User registration failed: {errorMessages}", 400);
                }

                var roleResult = await userManager.AddToRoleAsync(newUser, "Student");
                if (!roleResult.Succeeded)
                {
                    throw new ApiException("User registration succeeded but failed to assign role.", 500);
                }

                return true;
            }
        }
    }
}