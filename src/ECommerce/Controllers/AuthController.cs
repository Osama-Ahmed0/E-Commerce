using ECommerce.Data.Models;
using ECommerce.Dtos;
using ECommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        private readonly IAuthService _authService = authService;

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto model)
        {
            var result = await _authService.RegisterAsync(model, UserRole.Customer);
            if (!result.Succeeded) return BadRequest(result.Errors);
            return Ok();
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginDto model)
        {
            var response = await _authService.LoginAsync(model);
            if (response == null)
                return Unauthorized();
            ToCookies(response);
            return Ok(response);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            var token = Request.Cookies["refreshToken"];
            var response = await _authService.RefreshTokenAsync(token);
            if (response == null)
                return Unauthorized();

            if (!response.IsAuthenticated)
                return NotFound(response);

            ToCookies(response);
            return Ok(response);
        }
        [HttpPost("revoke")]
        public async Task<IActionResult> Revoke()
        {
            var token = Request.Cookies["refreshToken"];
            var response = await _authService.RevokeTokenAsync(token);

            if (response == null)
                return NotFound(new AuthResponseDto { Message = "Token not found or already inactive." });

            return Ok(response);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("admin/create-admin")]
        public async Task<IActionResult> CreateAdmin(RegisterDto model)
        {
            var result = await _authService.RegisterAsync(model, UserRole.Admin);
            if (!result.Succeeded) return BadRequest(result.Errors);
            return Ok();
        }
        private void ToCookies(AuthResponseDto response)
        {
            Response.Cookies.Append("refreshToken", response.RefreshToken!, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                Expires = response.RefreshTokenExpiration
            });
        }
    }
}
