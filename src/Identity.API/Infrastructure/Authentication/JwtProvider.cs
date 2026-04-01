using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Shared;

namespace Identity.API.Infrastructure.Authentication
{
    public class JwtProvider(IConfiguration configuration) : IJwtProvider
    {
        public string GenerateJwtToken(List<Claim> claims)
        {
            var secretKey = configuration["JWT_SECRET"] ?? throw new ArgumentNullException("JWT_SECRET is missing");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            
            var expiryMinutes = configuration.GetValue<double>("JWT_EXPIRY_MINUTES", 60);
            var expires = DateTime.UtcNow.AddMinutes(expiryMinutes);
            
            var token = new JwtSecurityToken(
                issuer: configuration["JWT_ISSUER"],
                audience: configuration["JWT_AUDIENCE"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );
            
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var secretKey = configuration["JWT_SECRET"] ?? throw new ArgumentNullException("JWT_SECRET is missing");
            
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
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
    }
}