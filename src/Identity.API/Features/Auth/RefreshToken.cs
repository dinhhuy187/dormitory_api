using System.Security.Claims;
using Carter;
using FluentValidation;
using Identity.API.Domain.Entities;
using Identity.API.Infrastructure.Authentication;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shared;

namespace Identity.API.Features.Auth
{
    public static class RefreshToken
    {
        public record Command(string AccessToken, string RefreshToken) : IRequest<ApiResponse<Response>>;
        public record Response(string Token, string RefreshToken);
        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.AccessToken).NotEmpty().WithMessage("Access token không được để trống");
                RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("Refresh token không được để trống");
            }
        }
        public class Endpoint : ICarterModule
        {
            public void AddRoutes(IEndpointRouteBuilder app)
            {
                app.MapPost("/api/auth/refresh-token", async ([FromBody] Command command, IMediator mediator) =>
                {
                    var result = await mediator.Send(command);
                    return Results.Ok(result);
                })
                .WithTags("Auth")
                .WithName("RefreshToken")
                .Produces<Response>(StatusCodes.Status200OK);
            }
        }
        public class Handler(
            IJwtProvider jwtProvider,
            UserManager<ApplicationUser> userManager
            ) : IRequestHandler<Command, ApiResponse<Response>>
        {
            public async Task<ApiResponse<Response>> Handle(Command request, CancellationToken cancellationToken)
            {
                var principal = jwtProvider.GetPrincipalFromExpiredToken(request.AccessToken);

                var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userId))
                {
                    throw new ApiException("Token không hợp lệ.", 401);
                }

                var user = await userManager.FindByIdAsync(userId);

                if (user == null ||
                    user.RefreshToken != request.RefreshToken ||
                    user.RefreshTokenExpiryTimeUtc <= DateTime.UtcNow)
                {
                    throw new ApiException("Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại", 401);
                }

                var newAccessToken = jwtProvider.GenerateJwtToken(principal.Claims.ToList());
                var newRefreshToken = Guid.NewGuid().ToString();

                user.RefreshToken = newRefreshToken;
                await userManager.UpdateAsync(user);

                return new ApiResponse<Response>(new Response(newAccessToken, newRefreshToken));
            }
        }
    }
}