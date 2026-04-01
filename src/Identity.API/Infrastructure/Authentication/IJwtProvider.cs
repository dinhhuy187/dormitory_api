using System.Security.Claims;

namespace Identity.API.Infrastructure.Authentication
{
    public interface IJwtProvider
    {
        string GenerateJwtToken(List<Claim> claims);
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    }
}