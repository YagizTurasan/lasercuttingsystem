using LaserCuttingSystem.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace LaserCuttingSystem.Identity.Interfaces
{
    public interface IJwtTokenService
    {
        Task<TokenResult> GenerateTokenAsync(ApplicationUser user);
        Task<string> GenerateAccessTokenAsync(ApplicationUser user);
        Task<string> GenerateRefreshTokenAsync();
        Task<(string AccessToken, string RefreshToken)> GenerateTokenPairAsync(ApplicationUser user);
        ClaimsPrincipal? ValidateToken(string token);
        Task<ClaimsPrincipal?> ValidateTokenAsync(string token);
        Task<bool> ValidateRefreshTokenAsync(string refreshToken, Guid userId);
        Task<(string AccessToken, string RefreshToken)?> RefreshTokenAsync(string refreshToken);
        Task RevokeRefreshTokenAsync(string refreshToken);
        Task RevokeAllUserRefreshTokensAsync(Guid userId);
        string? GetUserIdFromToken(string token);
        bool IsTokenExpired(string token);

        // Şifre sıfırlama ve e-posta doğrulama token'ları ASP.NET Identity'nin token sağlayıcılarıyla üretilir (UserManager)
    }
}
