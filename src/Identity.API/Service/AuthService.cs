using Identity.API.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Shared;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Identity.API.Service
{
    public class AuthService(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration) : IAuthService
    {
        public async Task<ApiResponse<object>> LoginAsync(LoginRequestDto request)
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null || !user.IsActive)
            {
                return ApiResponse<object>.FailureResponse(new List<string> { "Invalid credentials" }, "Login failed");
            }

            var isPasswordValid = await userManager.CheckPasswordAsync(user, request.Password);
            if (!isPasswordValid)
            {
                return ApiResponse<object>.FailureResponse(new List<string> { "Invalid credentials" }, "Login failed");
            }

            var role = await userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim("FullName", user.FullName),
                new Claim(ClaimTypes.Role, role.FirstOrDefault() ?? string.Empty)
            };

            var accessToken = GenerateJwtToken(claims);
            var refreshToken = Guid.NewGuid().ToString();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTimeUtc = DateTime.UtcNow.AddDays(Convert.ToDouble(configuration["JWT_REFRESH_EXPIRY_DAYS"] ?? "14"));

            return ApiResponse<object>.SuccessResponse(new AuthResponseDto
            {
                Token = accessToken,
                RefreshToken = refreshToken,
            }, "Login successful");
        }

        public async Task<bool> RegisterAsync(RegisterRequestDto request)
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

            await userManager.AddToRoleAsync(newUser, "Student");
            return true;
        }

        private string GenerateJwtToken(List<Claim> claims)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT_SECRET"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(configuration["JWT_EXPIRY_MINUTES"] ?? "60"));
            var token = new JwtSecurityToken(
                issuer: configuration["JWT_ISSUER"],
                audience: configuration["JWT_AUDIENCE"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
