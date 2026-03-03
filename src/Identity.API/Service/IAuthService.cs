using Identity.API.DTOs;
using Shared;

namespace Identity.API.Service
{
    public interface IAuthService
    {
        Task<ApiResponse<object>> LoginAsync(LoginRequestDto request);
        Task<bool> RegisterAsync(RegisterRequestDto request);
    }
}
