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
        public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto request)
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null || !user.IsActive)
            {
                throw new ApiException("Invalid credentials", 401);
            }

            var isPasswordValid = await userManager.CheckPasswordAsync(user, request.Password);
            if (!isPasswordValid)
            {
                throw new ApiException("Invalid credentials", 401);
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
            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded) throw new ApiException("Lỗi khi cập nhật phiên đăng nhập", 500);

            return new ApiResponse<AuthResponseDto>(new AuthResponseDto
            {
                Token = accessToken,
                RefreshToken = refreshToken,
            });
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

        public async Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request)
        {
            var principal = GetPrincipalFromExpiredToken(request.AccessToken);
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

            var user = await userManager.FindByIdAsync(userId);

            if (user == null ||
                user.RefreshToken != request.RefreshToken ||
                user.RefreshTokenExpiryTimeUtc <= DateTime.UtcNow)
            {
                throw new ApiException("Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại", 401);
            }

            var newAccessToken = GenerateJwtToken(principal.Claims.ToList());
            var newRefreshToken = Guid.NewGuid().ToString();

            user.RefreshToken = newRefreshToken;
            await userManager.UpdateAsync(user);

            return new ApiResponse<AuthResponseDto>(new AuthResponseDto
            {
                Token = newAccessToken,
                RefreshToken = newRefreshToken
            });
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT_SECRET"]!)),
                ValidateLifetime = false,
                ValidIssuer = configuration["JWT_ISSUER"],
                ValidAudience = configuration["JWT_AUDIENCE"]
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken || 
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new ApiException("Invalid token", 401);
            }
            return principal;
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
