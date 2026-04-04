using System.Security.Claims;
using Shared.Endpoints;
using FluentValidation;
using Identity.API.Domain.Entities;
using Identity.API.Infrastructure.Authentication;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shared;

namespace Identity.API.Features.Auth
{
    public static class Login
    {
        public record Command(string Email, string Password) : IRequest<ApiResponse<Response>>;

        public record Response(string Token, string RefreshToken);

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.Email).NotEmpty().WithMessage("Email không được để trống")
                .EmailAddress().WithMessage("Email không hợp lệ");
                RuleFor(x => x.Password).NotEmpty().WithMessage("Mật khẩu không được để trống");
            }
        }

        public class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapPost("/api/auth/login", async ([FromBody] Command command, IMediator mediator) =>
                {
                    var result = await mediator.Send(command);
                    return Results.Ok(result);
                })
                .WithTags("Auth")
                .WithName("Login")
                .Produces<Response>(StatusCodes.Status200OK);
            }
        }

        public class Handler(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            IJwtProvider jwtProvider) : IRequestHandler<Command, ApiResponse<Response>>
        {
            public async Task<ApiResponse<Response>> Handle(Command request, CancellationToken cancellationToken)
            {
                // Kiểm tra User có tồn tại không
                var user = await userManager.FindByEmailAsync(request.Email);
                if (user == null || !user.IsActive)
                {
                    throw new ApiException("Invalid credentials", 401);
                }

                // Kiểm tra Password
                var isPasswordValid = await userManager.CheckPasswordAsync(user, request.Password);
                if (!isPasswordValid)
                {
                    throw new ApiException("Invalid credentials", 401);
                }

                // Lấy Role để đóng gói vào Token
                var roles = await userManager.GetRolesAsync(user);
                
                // Khởi tạo các Claims
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, user.Id),
                    new(ClaimTypes.Email, user.Email!),
                    new("FullName", user.FullName),
                    new(ClaimTypes.Role, roles.FirstOrDefault() ?? string.Empty)
                };

                // Tạo Token (Gọi hàm từ IJwtProvider dùng chung)
                var accessToken = jwtProvider.GenerateJwtToken(claims);
                var refreshToken = Guid.NewGuid().ToString();

                // Lưu Refresh Token vào Database
                user.RefreshToken = refreshToken;
                
                var expiryDays = configuration.GetValue<double>("JWT_REFRESH_EXPIRY_DAYS", 14);
                user.RefreshTokenExpiryTimeUtc = DateTime.UtcNow.AddDays(expiryDays);
                
                var result = await userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    throw new ApiException("Lỗi khi cập nhật phiên đăng nhập hệ thống.", StatusCodes.Status500InternalServerError);
                }

                return new ApiResponse<Response>(new Response(accessToken, refreshToken));
            }
        }
    }
}