using ECommerce.Configuration;
using ECommerce.Data.Models;
using ECommerce.Dtos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ECommerce.Services
{
    public class AuthService(UserManager<User> userManager, JwtOptions jwtOptions) : IAuthService
    {
        private readonly UserManager<User> userManager = userManager;
        private readonly JwtOptions jwtOptions = jwtOptions;

        public async Task<IdentityResult> RegisterAsync(RegisterDto model, UserRole role)
        {
            var user = new User { UserName = model.UserName, Email = model.Email, FullName = model.FullName, Role = role };
            var result = await userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded) return result;

            var roleResult = await userManager.AddToRoleAsync(user, role.ToString());
            if (!roleResult.Succeeded)
            {
                await userManager.DeleteAsync(user);
                return roleResult;
            }

            return result;
        }

        public async Task<AuthResponseDto?> LoginAsync(UserLoginDto model)
        {
            var user = await userManager.FindByNameAsync(model.UserName);
            if (user == null) return null;
            if (!await userManager.CheckPasswordAsync(user, model.Password)) return null;

            var accessToken = await GenerateJwtTokenAsync(user);
            var refreshToken = GenerateRefreshToken();

            user.RefreshTokens?.Add(refreshToken);
            await userManager.UpdateAsync(user);

            return new AuthResponseDto
            {
                Token = accessToken,
                UserName = user.UserName,
                Email = user.Email,
                Role = user.Role.ToString(),
                IsAuthenticated = true,
                RefreshToken = refreshToken.Token,
                RefreshTokenExpiration = refreshToken.ExpiresOn
            };
        }

        public async Task<AuthResponseDto?> RefreshTokenAsync(string token)
        {
            var user = await userManager.Users.FirstOrDefaultAsync(u => u.RefreshTokens!.Any(t => t.Token == token));

            if (user == null)
                return new AuthResponseDto { IsAuthenticated = false, Message = "Invalid refresh token" };

            var refreshToken = user.RefreshTokens?.FirstOrDefault(t => t.Token == token);

            if (refreshToken == null || !refreshToken.IsActive)
                return new AuthResponseDto { IsAuthenticated = false, Message = "Refresh token expired" };

            var newAccessToken = await GenerateJwtTokenAsync(user);
            var newRefreshToken = GenerateRefreshToken();

            refreshToken.RevokedOn = DateTime.UtcNow;

            user.RefreshTokens?.Add(newRefreshToken);
            await userManager.UpdateAsync(user);

            return new AuthResponseDto
            {
                Token = newAccessToken,
                UserName = user.UserName,
                Email = user.Email,
                Role = user.Role.ToString(),
                IsAuthenticated = true,
                RefreshToken = newRefreshToken.Token,
                RefreshTokenExpiration = newRefreshToken.ExpiresOn
            };
        }

        public async Task<AuthResponseDto?> RevokeTokenAsync(string token)
        {
            var user = await userManager.Users.FirstOrDefaultAsync(u => u.RefreshTokens!.Any(t => t.Token == token));
            if (user == null)
                return null;
            var refreshToken = user.RefreshTokens?.FirstOrDefault(t => t.Token == token);
            if (refreshToken == null || !refreshToken.IsActive)
                return null;
            refreshToken.RevokedOn = DateTime.UtcNow;
            await userManager.UpdateAsync(user);
            return new AuthResponseDto { Message = "Token revoked" };
        }

        private async Task<string> GenerateJwtTokenAsync(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(jwtOptions.SigningKey
                ?? throw new InvalidOperationException("JWT signing key is not configured."));

            var claims = new List<Claim>
            {
                new (JwtRegisteredClaimNames.Jti, user.Id),
                new (JwtRegisteredClaimNames.UniqueName, user.UserName!),
                new (JwtRegisteredClaimNames.Email, user.Email!),
                new (ClaimTypes.Role, user.Role.ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = jwtOptions.Issuer,
                Audience = jwtOptions.Audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256),
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(jwtOptions.Lifetime),
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private RefreshToken GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return new RefreshToken
            {
                Token = Convert.ToBase64String(randomNumber),
                CreatedOn = DateTime.UtcNow,
                ExpiresOn = DateTime.UtcNow.AddDays(7)
            };
        }
    }
}
