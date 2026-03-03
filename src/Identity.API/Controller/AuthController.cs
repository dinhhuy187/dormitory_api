using Identity.API.DTOs;
using Identity.API.Service;
using Microsoft.AspNetCore.Mvc;
using Shared;

namespace Identity.API.Controller
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var result = await authService.LoginAsync(request);
            if (result.Success)
            {
                return Ok(result);
            }
            return Unauthorized(result);
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            await authService.RegisterAsync(request);
            return Ok(ApiResponse<string>.SuccessResponse(null, "User registered successfully"));
        }
    }
}
