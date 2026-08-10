using ECommerce.Data.Models;
using ECommerce.Dtos;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;

namespace ECommerce.Services
{
    public interface IAuthService
    {
        Task<IdentityResult> RegisterAsync(RegisterDto model, UserRole role);
        Task<AuthResponseDto?> LoginAsync(UserLoginDto model);
        Task<AuthResponseDto?> RefreshTokenAsync(string token);
        Task<AuthResponseDto?> RevokeTokenAsync(string token);
    }
}
